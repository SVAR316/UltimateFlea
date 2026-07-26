using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace UltimateFlea.Economy;

// Пишем в Templates.Prices и ItemPriceOverrideRouble, новые офферы подхватят.
[Injectable(InjectionType.Singleton)]
public class PriceApplier(
    DatabaseServer databaseServer,
    ConfigServer configServer)
{
    public void SetPrice(MongoId tpl, double price)
    {
        var rounded = Math.Max(1, Math.Round(price));

        var prices = databaseServer.GetTables().Templates.Prices;
        prices[tpl] = rounded;

        var ragfairConfig = configServer.GetConfig<RagfairConfig>();
        ragfairConfig.Dynamic.ItemPriceOverrideRouble[tpl] = rounded;
    }
}
