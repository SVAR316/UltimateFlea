using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using UltimateFlea.Util;

namespace UltimateFlea.Pricing;

internal enum FetchStatus
{
    Ok,
    NotModified,
    Failed
}

internal sealed record FetchOutcome<T>(
    FetchStatus Status,
    T? Value,
    string? ETag,
    string? LastModified,
    string? FailureStage,
    string? FailureDetail)
{
    public static FetchOutcome<T> Fail(string stage, string detail) => new(FetchStatus.Failed, default, null, null, stage, detail);
}

/// <summary>
/// Shared HTTP transport for the remote price feeds: gzip/deflate, conditional requests,
/// response streaming, a per-attempt timeout and exponential backoff.
/// </summary>
internal static class PriceHttp
{
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(2);

    // AutomaticDecompression makes the handler send "Accept-Encoding: gzip, deflate" and inflate
    // the body transparently. The timeout lives on a per-attempt CancellationTokenSource instead of
    // HttpClient.Timeout, because that one cannot be changed after the first request while the
    // timeout is a config value.
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        ConnectTimeout = TimeSpan.FromSeconds(30)
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public static FetchOutcome<T> Get<T>(
        string url,
        string label,
        TimeSpan attemptTimeout,
        int retries,
        Action<HttpRequestMessage> configure,
        string? etag,
        string? lastModified,
        Func<Stream, CancellationToken, Task<T?>> read,
        Action<string> warn) where T : class
    {
        var total = Stopwatch.StartNew();
        var last = FetchOutcome<T>.Fail("unknown", "no attempt was made");

        for (var i = 0; i <= retries; i++)
        {
            if (i > 0)
            {
                var delay = RetryBaseDelay * Math.Pow(2, i - 1);

                // A slow line can burn a full timeout per attempt; stop before startup hangs for ages.
                if (total.Elapsed + delay >= attemptTimeout * 2)
                {
                    break;
                }

                Thread.Sleep(delay);
            }

            using var cts = new CancellationTokenSource(attemptTimeout);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                configure(request);
                if (!string.IsNullOrEmpty(etag))
                {
                    request.Headers.TryAddWithoutValidation("If-None-Match", etag);
                }

                if (!string.IsNullOrEmpty(lastModified))
                {
                    request.Headers.TryAddWithoutValidation("If-Modified-Since", lastModified);
                }

                // Headers only: the body is parsed straight off the network stream.
                using var response = Client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .GetAwaiter().GetResult();

                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    return new FetchOutcome<T>(FetchStatus.NotModified, null, etag, lastModified, null, null);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var code = (int)response.StatusCode;
                    last = FetchOutcome<T>.Fail($"http {code}", $"HTTP {code}");
                    warn($"{label}: stage 'http' failed, HTTP {code}, attempt {i + 1}/{retries + 1}");

                    // Client errors (bad key, bad URL) will not fix themselves between attempts.
                    if (code is >= 400 and < 500 && code is not 408 and not 429)
                    {
                        return last;
                    }

                    continue;
                }

                using var stream = response.Content.ReadAsStreamAsync(cts.Token).GetAwaiter().GetResult();
                var value = read(stream, cts.Token).GetAwaiter().GetResult();
                if (value is null)
                {
                    warn($"{label}: stage 'parse' failed, unexpected response format.");
                    return FetchOutcome<T>.Fail("parse", "unexpected response format");
                }

                var newEtag = response.Headers.ETag?.ToString();
                var newModified = response.Content.Headers.LastModified?.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
                return new FetchOutcome<T>(FetchStatus.Ok, value, newEtag, newModified, null, null);
            }
            catch (Exception ex)
            {
                var stage = ClassifyFailure(ex, cts);
                last = FetchOutcome<T>.Fail(stage, ex.Message);
                warn($"{label}: stage '{stage}' failed, attempt {i + 1}/{retries + 1}: {ex.Message}");
            }
        }

        return last;
    }

    private static string ClassifyFailure(Exception ex, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested || ex is TimeoutException)
        {
            return "timeout";
        }

        if (ex is HttpRequestException http)
        {
            switch (http.HttpRequestError)
            {
                case HttpRequestError.NameResolutionError:
                    return "dns";
                case HttpRequestError.SecureConnectionError:
                    return "tls";
                case HttpRequestError.ConnectionError:
                    return "connect";
            }
        }

        for (var inner = ex; inner is not null; inner = inner.InnerException)
        {
            if (inner is AuthenticationException)
            {
                return "tls";
            }

            if (inner is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData })
            {
                return "dns";
            }

            if (inner is JsonException)
            {
                return "parse";
            }
        }

        return "network";
    }
}

/// <summary>Result of one live fetch, before it is published.</summary>
internal sealed record LiveResult(
    Dictionary<MongoId, double>? Prices,
    bool FromCache,
    string? ETag,
    string? LastModified,
    string? FailureStage);

/// <summary>
/// Common behaviour of the online price sources: the live feed is tried first, then the disk
/// cache, then the previously loaded prices stay in place. The router falls back to local.
/// </summary>
public abstract class RemotePriceSource<TSelf>(ISptLogger<TSelf> logger, ConfigManager configManager) : IPriceSource
{
    // Published as a single reference: the tick thread either sees the old map or the new one.
    private volatile IReadOnlyDictionary<MongoId, double> _prices = new Dictionary<MongoId, double>();

    protected ISptLogger<TSelf> Log { get; } = logger;
    protected ConfigManager Config { get; } = configManager;

    public abstract string Id { get; }
    public int CachedCount => _prices.Count;

    /// <summary>Short name used as the log prefix.</summary>
    protected abstract string Label { get; }

    protected abstract string CachePath { get; }

    internal abstract LiveResult FetchLive();

    internal virtual void PersistValidators(LiveResult result)
    {
    }

    protected TimeSpan FetchTimeout => TimeSpan.FromSeconds(Math.Clamp(Config.Mod.PriceFetchTimeoutSeconds, 10, 1800));

    public void Refresh()
    {
        // Build into a local map and swap it in only on success: if both the live feed and the
        // cache fail, the previously loaded prices must stay in place.
        var live = FetchLive();
        if (live.Prices is { Count: > 0 } fresh)
        {
            _prices = fresh;
            if (live.FromCache)
            {
                Log.Info($"[UltimateFlea] {Label}: not modified (HTTP 304), {fresh.Count} cached prices in effect.");
            }
            else
            {
                Log.Info($"[UltimateFlea] {Label}: downloaded {fresh.Count} prices.");
                if (SaveCache(fresh))
                {
                    PersistValidators(live);
                }
            }

            return;
        }

        var stage = live.FailureStage ?? "unknown";
        var disk = LoadFromDisk();
        if (disk is not null)
        {
            _prices = disk;
            Log.Warning($"[UltimateFlea] {Label}: live fetch failed at stage '{stage}', using disk cache ({disk.Count} prices in effect).");
            return;
        }

        Log.Warning($"[UltimateFlea] {Label}: live fetch failed at stage '{stage}' and no usable disk cache, keeping {_prices.Count} known prices in effect.");
    }

    public double? GetBasePrice(MongoId tpl)
    {
        return _prices.TryGetValue(tpl, out var price) && price > 0 ? price : null;
    }

    protected Dictionary<MongoId, double>? LoadFromDisk()
    {
        if (string.IsNullOrEmpty(Config.ModPath))
        {
            Log.Error($"[UltimateFlea] Mod path is not known yet, cannot read the {Id} cache.");
            return null;
        }

        var data = JsonFileStore.ReadOrDefault<Dictionary<string, double>, TSelf>(
            CachePath,
            JsonFileStore.Data,
            Log,
            $"{Id} price cache");

        if (data is null || data.Count == 0)
        {
            return null;
        }

        var prices = new Dictionary<MongoId, double>(data.Count);
        foreach (var (id, price) in data)
        {
            if (price > 0 && MongoIds.TryParse(id, Log, $"{Id} price cache", out var tpl))
            {
                prices[tpl] = price;
            }
        }

        return prices.Count > 0 ? prices : null;
    }

    private bool SaveCache(Dictionary<MongoId, double> prices)
    {
        if (string.IsNullOrEmpty(Config.ModPath))
        {
            Log.Error($"[UltimateFlea] Mod path is not known yet, skipping the {Id} cache write.");
            return false;
        }

        var data = prices.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
        return JsonFileStore.WriteAtomic(CachePath, data, JsonFileStore.Data, Log, $"{Id} price cache", out _);
    }
}

/// <summary>Validators of the cached tarkov.dev dump, stored next to the price cache.</summary>
internal sealed class FeedValidators
{
    [JsonPropertyName("etag")]
    public string? ETag { get; set; }

    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }
}

// Prices from the static JSON dump at json.tarkov.dev (avg24hPrice, fallback lastLowPrice).
// This is NOT the GraphQL API: api.tarkov.dev/graphql is never called by this mod, so an outage
// there does not affect prices. The dump is ~17 MB uncompressed, hence gzip, streaming, a long
// timeout and ETag / If-Modified-Since revalidation.
[Injectable(InjectionType.Singleton)]
public class TarkovDevPriceSource(
    ISptLogger<TarkovDevPriceSource> logger,
    ConfigManager configManager) : RemotePriceSource<TarkovDevPriceSource>(logger, configManager)
{
    private const string JsonApiBase = "https://json.tarkov.dev/";

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public override string Id => "tarkovdev";
    protected override string Label => "json.tarkov.dev (static dump, GraphQL API not used)";

    private string GameMode => Config.Mod.PvePrices ? "pve" : "regular";
    private string ItemsUrl => $"{JsonApiBase}{GameMode}/items";
    protected override string CachePath => Path.Combine(JsonFileStore.DataDir(Config.ModPath), $"tarkovdev_prices-{GameMode}.json");
    private string ValidatorsPath => CachePath + ".meta";

    internal override LiveResult FetchLive()
    {
        // Revalidate only when a usable disk cache exists to fall back on for a 304.
        string? etag = null, lastModified = null;
        Dictionary<MongoId, double>? cached = null;
        if (!string.IsNullOrEmpty(Config.ModPath))
        {
            var meta = JsonFileStore.ReadOrDefault<FeedValidators, TarkovDevPriceSource>(
                ValidatorsPath, JsonFileStore.Data, Log, "tarkovdev cache validators");
            if (meta is not null && (!string.IsNullOrEmpty(meta.ETag) || !string.IsNullOrEmpty(meta.LastModified)))
            {
                cached = LoadFromDisk();
                if (cached is not null)
                {
                    etag = meta.ETag;
                    lastModified = meta.LastModified;
                }
            }
        }

        var outcome = PriceHttp.Get(
            ItemsUrl,
            "[UltimateFlea] json.tarkov.dev",
            FetchTimeout,
            retries: 3,
            configure: _ => { },
            etag,
            lastModified,
            ReadAsync,
            msg => Log.Warning(msg));

        switch (outcome.Status)
        {
            case FetchStatus.NotModified when cached is not null:
                return new LiveResult(cached, true, etag, lastModified, null);

            case FetchStatus.Ok when outcome.Value is { } root:
                var prices = ToPrices(root);
                return prices.Count > 0
                    ? new LiveResult(prices, false, outcome.ETag, outcome.LastModified, null)
                    : new LiveResult(null, false, null, null, "parse (no usable prices)");

            default:
                return new LiveResult(null, false, null, null, outcome.FailureStage);
        }
    }

    internal override void PersistValidators(LiveResult result)
    {
        if (string.IsNullOrEmpty(Config.ModPath))
        {
            return;
        }

        // Written after the price cache, so validators never describe newer data than the cache holds.
        JsonFileStore.WriteAtomic(
            ValidatorsPath,
            new FeedValidators { ETag = result.ETag, LastModified = result.LastModified },
            JsonFileStore.Data,
            Log,
            "tarkovdev cache validators",
            out _);
    }

    private static async Task<DevRoot?> ReadAsync(Stream stream, CancellationToken ct)
    {
        var root = await JsonSerializer.DeserializeAsync<DevRoot>(stream, ParseOptions, ct);
        return root?.Data?.Items is null ? null : root;
    }

    private Dictionary<MongoId, double> ToPrices(DevRoot root)
    {
        var prices = new Dictionary<MongoId, double>();
        var skipped = 0;

        foreach (var (id, item) in root.Data!.Items!)
        {
            // The feed also carries non-item keys; warning per entry would flood the log,
            // so these are only counted.
            if (item is null || !MongoId.IsValidMongoId(id))
            {
                skipped++;
                continue;
            }

            var price = item.Avg24hPrice is > 0 ? item.Avg24hPrice : item.LastLowPrice;
            if (price is > 0)
            {
                prices[new MongoId(id)] = price.Value;
            }
        }

        if (skipped > 0)
        {
            Log.Debug($"[UltimateFlea] json.tarkov.dev: skipped {skipped} entries without a valid item id.");
        }

        return prices;
    }

    private sealed class DevRoot
    {
        [JsonPropertyName("data")]
        public DevData? Data { get; set; }
    }

    private sealed class DevData
    {
        [JsonPropertyName("items")]
        public Dictionary<string, DevItem?>? Items { get; set; }
    }

    private sealed class DevItem
    {
        [JsonPropertyName("avg24hPrice")]
        public double? Avg24hPrice { get; set; }

        [JsonPropertyName("lastLowPrice")]
        public double? LastLowPrice { get; set; }
    }
}

// OPTIONAL second provider, off unless priceSource = "tarkovmarket" AND tarkovMarketApiKey is set.
// UNVERIFIED: the API answered 401 without a key, so the auth header and the response schema could
// not be checked. The shape handled here (array of objects with bsgId + avg24hPrice/price, or an
// object wrapping such an array / keyed by id) is what the public docs suggest. PvE support at
// tarkov-market is unconfirmed: no game-mode parameter is sent, so these may be regular prices.
// Anything that does not parse into MinUsablePrices entries is rejected instead of wiping prices.
[Injectable(InjectionType.Singleton)]
public class TarkovMarketPriceSource(
    ISptLogger<TarkovMarketPriceSource> logger,
    ConfigManager configManager) : RemotePriceSource<TarkovMarketPriceSource>(logger, configManager)
{
    private const string ItemsUrl = "https://api.tarkov-market.app/api/v1/items/all";
    private const int MinUsablePrices = 300;

    public override string Id => "tarkovmarket";
    protected override string Label => "api.tarkov-market.app (unverified schema)";
    protected override string CachePath => Path.Combine(JsonFileStore.DataDir(Config.ModPath), "tarkovmarket_prices.json");

    internal override LiveResult FetchLive()
    {
        var key = Config.Mod.TarkovMarketApiKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return new LiveResult(null, false, null, null, "no api key");
        }

        // The key is never logged.
        var outcome = PriceHttp.Get(
            ItemsUrl,
            "[UltimateFlea] tarkov-market",
            FetchTimeout,
            retries: 2,
            configure: r => r.Headers.TryAddWithoutValidation("x-api-key", key),
            etag: null,
            lastModified: null,
            (stream, ct) => JsonDocument.ParseAsync(stream, cancellationToken: ct).ContinueWith(t => (JsonDocument?)t.Result, ct),
            msg => Log.Warning(msg));

        if (outcome.Status != FetchStatus.Ok || outcome.Value is not { } doc)
        {
            return new LiveResult(null, false, null, null, outcome.FailureStage);
        }

        using (doc)
        {
            var prices = Parse(doc.RootElement, out var skipped);
            if (prices.Count < MinUsablePrices)
            {
                Log.Error(
                    $"[UltimateFlea] tarkov-market: response parsed into only {prices.Count} usable prices " +
                    $"(need at least {MinUsablePrices}, {skipped} entries skipped). The response schema is unverified; " +
                    "ignoring it and falling back to the cache / other sources.");
                return new LiveResult(null, false, null, null, "parse (too few usable prices)");
            }

            return new LiveResult(prices, false, null, null, null);
        }
    }

    private static Dictionary<MongoId, double> Parse(JsonElement root, out int skipped)
    {
        var prices = new Dictionary<MongoId, double>();
        skipped = 0;

        foreach (var (key, item) in Entries(root))
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                skipped++;
                continue;
            }

            var id = ReadString(item, "bsgId") ?? ReadString(item, "bsgid") ?? key;
            var price = ReadNumber(item, "avg24hPrice") ?? ReadNumber(item, "price");
            if (id is null || !MongoId.IsValidMongoId(id) || price is not > 0)
            {
                skipped++;
                continue;
            }

            prices[new MongoId(id)] = price.Value;
        }

        return prices;
    }

    // Array of items, or an object that wraps one ("items"/"data") or is keyed by item id.
    private static IEnumerable<(string? Key, JsonElement Item)> Entries(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var wrapper in new[] { "items", "data" })
            {
                if (root.TryGetProperty(wrapper, out var inner) &&
                    inner.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                {
                    root = inner;
                    break;
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in root.EnumerateArray())
            {
                yield return (null, element);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                yield return (property.Name, property.Value);
            }
        }
    }

    private static string? ReadString(JsonElement item, string name) =>
        item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? ReadNumber(JsonElement item, string name) =>
        item.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.GetDouble() > 0
            ? v.GetDouble()
            : null;
}
