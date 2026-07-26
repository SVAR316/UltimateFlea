using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace UltimateFlea.Pricing;

[Injectable(InjectionType.Singleton)]
public class LocalPriceSource(
    ISptLogger<LocalPriceSource> logger,
    DatabaseServer databaseServer) : IPriceSource
{
    public string Id => "local";

    private readonly Dictionary<MongoId, double> _handbookPrices = new();

    public void Refresh()
    {
        _handbookPrices.Clear();

        var handbook = databaseServer.GetTables().Templates.Handbook;
        foreach (var item in handbook.Items)
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
        var prices = databaseServer.GetTables().Templates.Prices;
        if (prices.TryGetValue(tpl, out var fleaPrice) && fleaPrice > 0)
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
