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

    // Swapped in as one reference, like the remote source, so readers never see a half-filled map.
    private volatile IReadOnlyDictionary<MongoId, double> _handbookPrices = new Dictionary<MongoId, double>();

    public void Refresh()
    {
        var prices = new Dictionary<MongoId, double>();

        foreach (var item in templates.Handbook.Items)
        {
            if (item.Price is > 0)
            {
                prices[item.Id] = item.Price.Value;
            }
        }

        _handbookPrices = prices;

        logger.Debug($"[UltimateFlea] LocalPriceSource cached {prices.Count} handbook prices");
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
