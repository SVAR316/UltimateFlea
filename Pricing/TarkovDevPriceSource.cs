using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace UltimateFlea.Pricing;

// Цены с json.tarkov.dev (avg24hPrice).
[Injectable(InjectionType.Singleton)]
public class TarkovDevPriceSource(
    ISptLogger<TarkovDevPriceSource> logger,
    ConfigManager configManager) : IPriceSource
{
    private const string JsonApiBase = "https://json.tarkov.dev/";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly Dictionary<MongoId, double> _prices = new();

    public string Id => "tarkovdev";
    public int CachedCount => _prices.Count;

    private string GameMode => configManager.Mod.PvePrices ? "pve" : "regular";
    private string CachePath => Path.Combine(configManager.ModPath, "data", $"tarkovdev_prices-{GameMode}.json");
    private string ItemsUrl => $"{JsonApiBase}{GameMode}/items";

    public void Refresh()
    {
        _prices.Clear();

        if (TryLoadFromJsonApi())
        {
            SaveCache();
            return;
        }

        if (TryLoadFromDisk())
        {
            logger.Warning($"[UltimateFlea] json.tarkov.dev unavailable, using disk cache ({_prices.Count} prices).");
            return;
        }

        logger.Warning("[UltimateFlea] json.tarkov.dev unavailable and no cache found.");
    }

    public double? GetBasePrice(MongoId tpl)
    {
        return _prices.TryGetValue(tpl, out var price) && price > 0 ? price : null;
    }

    private bool TryLoadFromJsonApi()
    {
        var json = GetWithRetries(ItemsUrl, retries: 3);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Object)
            {
                logger.Warning("[UltimateFlea] json.tarkov.dev: unexpected response format.");
                return false;
            }

            foreach (var item in items.EnumerateObject())
            {
                if (!MongoId.IsValidMongoId(item.Name))
                {
                    continue;
                }

                if (!item.Value.TryGetProperty("avg24hPrice", out var avg) ||
                    avg.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                var price = avg.GetDouble();
                if (price > 0)
                {
                    _prices[new MongoId(item.Name)] = price;
                }
            }

            return _prices.Count > 0;
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] json.tarkov.dev parse fail: {ex.Message}");
            return false;
        }
    }

    private string? GetWithRetries(string url, int retries)
    {
        for (var i = 0; i <= retries; i++)
        {
            try
            {
                using var response = Http.GetAsync(url).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    logger.Warning($"[UltimateFlea] json.tarkov.dev HTTP {(int)response.StatusCode}, attempt {i + 1}/{retries + 1}");
                    continue;
                }

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.Warning($"[UltimateFlea] json.tarkov.dev fail, attempt {i + 1}/{retries + 1}: {ex.Message}");
            }
        }

        return null;
    }

    private bool TryLoadFromDisk()
    {
        try
        {
            if (!File.Exists(CachePath))
            {
                return false;
            }

            var data = JsonSerializer.Deserialize<Dictionary<string, double>>(File.ReadAllText(CachePath), JsonOptions);
            if (data is null || data.Count == 0)
            {
                return false;
            }

            foreach (var (id, price) in data)
            {
                if (price > 0 && MongoId.IsValidMongoId(id))
                {
                    _prices[new MongoId(id)] = price;
                }
            }

            return _prices.Count > 0;
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Failed to read tarkovdev cache: {ex.Message}");
            return false;
        }
    }

    private void SaveCache()
    {
        try
        {
            var dir = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var data = _prices.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            File.WriteAllText(CachePath, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Failed to save tarkovdev cache: {ex.Message}");
        }
    }
}
