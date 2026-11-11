using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace UltimateFlea.Pricing;

// Chooses the active source from mod.json. liveFlea=false means local only (no network);
// liveFlea=true uses the provider named by priceSource. Chain: live -> disk cache -> local.
[Injectable(InjectionType.Singleton)]
public class PriceSourceRouter(
    ISptLogger<PriceSourceRouter> logger,
    ConfigManager configManager,
    LocalPriceSource local,
    TarkovDevPriceSource tarkovDev,
    TarkovMarketPriceSource tarkovMarket) : IPriceSource
{
    // Resolved once per Refresh instead of per GetBasePrice call, and it reflects the fallback.
    private volatile IPriceSource _active = local;

    public string Id => _active.Id;

    private long _liveHits;
    private long _localFallbacks;

    /// <summary>True when a live provider (not local) is the active source.</summary>
    public bool IsLiveActive => !ReferenceEquals(_active, local);

    /// <summary>Number of live prices loaded by the last Refresh (0 when local is active).</summary>
    public int LivePriceCount { get; private set; }

    /// <summary>Returns and resets the per-lookup counters (live hit vs fallback to local).</summary>
    public (long Live, long Local) TakeLookupStats() =>
        (Interlocked.Exchange(ref _liveHits, 0), Interlocked.Exchange(ref _localFallbacks, 0));

    /// <summary>Deletes the live price caches (both game modes) and forgets the loaded live prices.</summary>
    public bool ResetLiveCache(List<string> deleted, List<string> errors)
    {
        var ok = tarkovDev.DeleteAllCaches(deleted, errors);
        if (ok)
        {
            tarkovDev.ClearLoaded();
        }

        return ok;
    }

    public void Refresh()
    {
        local.Refresh();

        var mod = configManager.Mod;
        if (!mod.ResolveLiveFlea())
        {
            _active = local;
            LivePriceCount = 0;
            logger.Info(
                $"[UltimateFlea] Price source: local handbook (liveFlea=false, priceSource='{mod.PriceSource}'; network not used).");
            return;
        }

        var wantsMarket = string.Equals(mod.PriceSource, "tarkovmarket", StringComparison.OrdinalIgnoreCase);
        if (wantsMarket && string.IsNullOrWhiteSpace(mod.TarkovMarketApiKey))
        {
            logger.Warning("[UltimateFlea] priceSource is 'tarkovmarket' but tarkovMarketApiKey is empty, using tarkovdev instead.");
            wantsMarket = false;
        }
        else if (!wantsMarket && !string.Equals(mod.PriceSource, "tarkovdev", StringComparison.OrdinalIgnoreCase))
        {
            logger.Info($"[UltimateFlea] liveFlea=true with priceSource='{mod.PriceSource}': using the default live provider tarkovdev.");
        }

        if (wantsMarket)
        {
            tarkovMarket.Refresh();
            if (tarkovMarket.CachedCount > 0)
            {
                _active = tarkovMarket;
                LivePriceCount = tarkovMarket.CachedCount;
                logger.Success($"[UltimateFlea] Price source: tarkovmarket ({tarkovMarket.CachedCount} prices, liveFlea=true).");
                return;
            }

            logger.Warning("[UltimateFlea] tarkovmarket returned 0 prices, trying tarkovdev.");
        }

        tarkovDev.Refresh();

        if (tarkovDev.CachedCount == 0)
        {
            _active = local;
            LivePriceCount = 0;
            logger.Warning("[UltimateFlea] Live prices unavailable (0 prices from live fetch and cache), falling back to local handbook prices.");
            return;
        }

        _active = tarkovDev;
        LivePriceCount = tarkovDev.CachedCount;
        logger.Success($"[UltimateFlea] Price source: tarkovdev ({tarkovDev.CachedCount} prices, {tarkovDev.SpreadCount} with a 24h low/high offer band, liveFlea=true).");
    }

    /// <summary>Per-item offer band from the live feed; null when local is active or the item has none.</summary>
    public PriceSpread? GetSpread(MongoId tpl) =>
        ReferenceEquals(_active, tarkovDev) ? tarkovDev.GetSpread(tpl) : null;

    /// <summary>Number of items with a live offer band in the active source.</summary>
    public int SpreadCount => ReferenceEquals(_active, tarkovDev) ? tarkovDev.SpreadCount : 0;

    public double? GetBasePrice(MongoId tpl)
    {
        if (ReferenceEquals(_active, local))
        {
            return local.GetBasePrice(tpl);
        }

        // Live feeds have no price for every template, so local stays the per-item fallback.
        var remote = _active.GetBasePrice(tpl);
        if (remote is > 0)
        {
            Interlocked.Increment(ref _liveHits);
            return remote;
        }

        Interlocked.Increment(ref _localFallbacks);
        return local.GetBasePrice(tpl);
    }
}
