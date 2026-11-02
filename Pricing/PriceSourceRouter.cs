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

    public void Refresh()
    {
        local.Refresh();

        var mod = configManager.Mod;
        if (!mod.ResolveLiveFlea())
        {
            _active = local;
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
                logger.Success($"[UltimateFlea] Price source: tarkovmarket ({tarkovMarket.CachedCount} prices, liveFlea=true).");
                return;
            }

            logger.Warning("[UltimateFlea] tarkovmarket returned 0 prices, trying tarkovdev.");
        }

        tarkovDev.Refresh();

        if (tarkovDev.CachedCount == 0)
        {
            _active = local;
            logger.Warning("[UltimateFlea] Live prices unavailable (0 prices from live fetch and cache), falling back to local handbook prices.");
            return;
        }

        _active = tarkovDev;
        logger.Success($"[UltimateFlea] Price source: tarkovdev ({tarkovDev.CachedCount} prices, liveFlea=true).");
    }

    public double? GetBasePrice(MongoId tpl)
    {
        if (ReferenceEquals(_active, local))
        {
            return local.GetBasePrice(tpl);
        }

        // Live feeds have no price for every template, so local stays the per-item fallback.
        var remote = _active.GetBasePrice(tpl);
        return remote is > 0 ? remote : local.GetBasePrice(tpl);
    }
}
