using System.Globalization;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using UltimateFlea.Util;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class TradeStatsStore(
    ISptLogger<TradeStatsStore> logger,
    ConfigManager configManager)
{
    private const int KeepDays = 30;
    private const string DayFormat = "yyyy-MM-dd";

    /// <summary>
    /// Buys and sells arrive on the trade request path, so writing the whole file per trade would
    /// put synchronous disk IO in front of the player. Writes are coalesced into this window and
    /// the economy tick calls <see cref="Flush"/>.
    /// </summary>
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    private readonly object _gate = new();
    private TradeStatsFile _data = new();
    private volatile bool _loaded;
    private bool _dirty;
    private DateTime _lastWriteUtc = DateTime.MinValue;

    private string PathFile => Path.Combine(JsonFileStore.DataDir(configManager.ModPath), "trade_stats.json");

    public void RecordBuy() => Bump(buy: true);
    public void RecordSell() => Bump(buy: false);

    /// <summary>Writes pending counters to disk. Cheap no-op when nothing changed.</summary>
    public void Flush()
    {
        lock (_gate)
        {
            if (_dirty)
            {
                SaveLocked();
            }
        }
    }

    public TradeStatsSnapshot GetSnapshot(int days = 14)
    {
        EnsureLoaded();
        lock (_gate)
        {
            var end = DateOnly.FromDateTime(DateTime.UtcNow);
            var series = new List<TradeDayPoint>();
            var buys = 0;
            var sells = 0;

            for (var i = days - 1; i >= 0; i--)
            {
                var key = DayKey(end.AddDays(-i));
                _data.Days.TryGetValue(key, out var point);
                var b = point?.Buys ?? 0;
                var s = point?.Sells ?? 0;
                buys += b;
                sells += s;
                series.Add(new TradeDayPoint(key, b, s));
            }

            return new TradeStatsSnapshot(series, buys, sells);
        }
    }

    private void Bump(bool buy)
    {
        EnsureLoaded();
        lock (_gate)
        {
            var key = DayKey(DateOnly.FromDateTime(DateTime.UtcNow));
            if (!_data.Days.TryGetValue(key, out var point) || point is null)
            {
                point = new TradeDayCounts();
                _data.Days[key] = point;
            }

            if (buy)
            {
                point.Buys++;
            }
            else
            {
                point.Sells++;
            }

            TrimLocked();
            _dirty = true;

            // Safety net in case nothing calls Flush() for a long stretch.
            if (DateTime.UtcNow - _lastWriteUtc >= FlushInterval)
            {
                SaveLocked();
            }
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_gate)
        {
            if (_loaded)
            {
                return;
            }

            if (string.IsNullOrEmpty(configManager.ModPath))
            {
                // Without a mod path we would read/write the server CWD; stay in memory only.
                logger.Error("[UltimateFlea] Mod path is not known yet, trade stats stay in memory.");
                _data = new TradeStatsFile();
                return;
            }

            _data = JsonFileStore.ReadOrDefault<TradeStatsFile, TradeStatsStore>(
                PathFile,
                JsonFileStore.Data,
                logger,
                "trade stats") ?? new TradeStatsFile();

            _loaded = true;
        }
    }

    private void TrimLocked()
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-(KeepDays - 1));
        foreach (var key in _data.Days.Keys.ToList())
        {
            // Invariant culture only: a culture-sensitive parse would fail to recognise our own
            // keys on some hosts and drop live data.
            if (!DateOnly.TryParseExact(key, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
                || day < cutoff)
            {
                _data.Days.Remove(key);
            }
        }
    }

    private void SaveLocked()
    {
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            logger.Error("[UltimateFlea] Mod path is not known yet, skipping trade stats save.");
            return;
        }

        if (JsonFileStore.WriteAtomic(PathFile, _data, JsonFileStore.Data, logger, "trade stats", out _))
        {
            _dirty = false;
            _lastWriteUtc = DateTime.UtcNow;
        }
    }

    private static string DayKey(DateOnly day) => day.ToString(DayFormat, CultureInfo.InvariantCulture);
}

public sealed class TradeStatsFile
{
    [JsonPropertyName("days")]
    public Dictionary<string, TradeDayCounts> Days { get; set; } = new();
}

public sealed class TradeDayCounts
{
    [JsonPropertyName("buys")]
    public int Buys { get; set; }

    [JsonPropertyName("sells")]
    public int Sells { get; set; }
}

public readonly record struct TradeDayPoint(string Day, int Buys, int Sells);

public readonly record struct TradeStatsSnapshot(
    IReadOnlyList<TradeDayPoint> Days,
    int TotalBuys,
    int TotalSells);
