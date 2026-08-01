using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace UltimateFlea.Pricing;

[Injectable(InjectionType.Singleton)]
public class LocalPriceSource(
    ISptLogger<LocalPriceSource> logger,
    TemplateTable templates) : IPriceSource
{
    public string Id => "local";

    private readonly Dictionary<MongoId, double> _handbookPrices = new();

    public void Refresh()
    {
        _handbookPrices.Clear();

        foreach (var item in templates.Handbook.Items)
        {
            if (item.Price is > 0)
            {
                _handbookPrices[item.Id] = item.Price.Value;
            }
        }

        logger.Debug($"[UltimateFlea] LocalPriceSource cached {_handbookPrices.Count} handbook prices");
    }

    public double? GetBasePrice(MongoId tpl)
    {
        if (templates.Prices.TryGetValue(tpl, out var fleaPrice) && fleaPrice > 0)
        {
            return fleaPrice;
        }

        if (_handbookPrices.TryGetValue(tpl, out var handbookPrice))
        {
            return handbookPrice;
        }

        return null;
    }
}
