using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Pricing;

// Выбирает local или tarkovdev по mod.json. Если tarkovdev пустой/упал, берём local.
[Injectable(InjectionType.Singleton)]
public class PriceSourceRouter(
    ISptLogger<PriceSourceRouter> logger,
    ConfigManager configManager,
    LocalPriceSource local,
    TarkovDevPriceSource tarkovDev) : IPriceSource
{
    public string Id => Active.Id;

    private IPriceSource Active =>
        string.Equals(configManager.Mod.PriceSource, "tarkovdev", StringComparison.OrdinalIgnoreCase)
            ? tarkovDev
            : local;

    public void Refresh()
    {
        local.Refresh();

        if (string.Equals(configManager.Mod.PriceSource, "tarkovdev", StringComparison.OrdinalIgnoreCase))
        {
            tarkovDev.Refresh();
            if (tarkovDev.CachedCount == 0)
            {
                logger.Warning("[UltimateFlea] tarkovdev returned 0 prices, falling back to local.");
            }
            else
            {
                logger.Success($"[UltimateFlea] Price source: tarkovdev ({tarkovDev.CachedCount} prices).");
            }
        }
        else
        {
            logger.Info("[UltimateFlea] Price source: local.");
        }
    }

    public double? GetBasePrice(MongoId tpl)
    {
        if (Active is TarkovDevPriceSource)
        {
            var remote = tarkovDev.GetBasePrice(tpl);
            if (remote is > 0)
            {
                return remote;
            }

            return local.GetBasePrice(tpl);
        }

        return local.GetBasePrice(tpl);
    }
}
