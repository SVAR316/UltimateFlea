using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace UltimateFlea.Economy;

// Пишем в Templates.Prices и ItemPriceOverrideRouble, новые офферы подхватят.
[Injectable(InjectionType.Singleton)]
public class PriceApplier(
    TemplateTable templates,
    RagfairConfig ragfairConfig)
{
    public void SetPrice(MongoId tpl, double price)
    {
        var rounded = Math.Max(1, Math.Round(price));

        templates.Prices[tpl] = rounded;
        ragfairConfig.Dynamic.ItemPriceOverrideRouble[tpl] = rounded;
    }
}
