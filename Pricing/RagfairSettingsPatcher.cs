using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace UltimateFlea.Pricing;

// Выключаем handbook regen, пол торговца и разброс 0.8-1.2, иначе наши цены не держатся.
[Injectable(InjectionType.Singleton)]
public class RagfairSettingsPatcher(
    ISptLogger<RagfairSettingsPatcher> logger,
    ConfigManager configManager,
    RagfairConfig ragfairConfig)
{
    public void Apply()
    {
        var mod = configManager.Mod;
        var dynamic = ragfairConfig.Dynamic;

        if (mod.PreserveBasePrices)
        {
            dynamic.GenerateBaseFleaPrices.UseHandbookPrice = false;
            dynamic.GenerateBaseFleaPrices.PreventPriceBeingBelowTraderBuyPrice = false;
            logger.Info("[UltimateFlea] Handbook price regeneration disabled.");
        }

        if (mod.DisableTraderPriceFloor)
        {
            dynamic.UseTraderPriceForOffersIfHigher = false;
            logger.Info("[UltimateFlea] Trader price floor disabled.");
        }

        if (mod.ExactOfferPrices)
        {
            dynamic.PriceRanges.Default.Min = 1.0;
            dynamic.PriceRanges.Default.Max = 1.0;
            dynamic.PriceRanges.Preset.Min = 1.0;
            dynamic.PriceRanges.Preset.Max = 1.0;
            dynamic.PriceRanges.Pack.Min = 1.0;
            dynamic.PriceRanges.Pack.Max = 1.0;
            logger.Info("[UltimateFlea] Offer price randomisation disabled (exact prices).");
        }

        if (mod.PlayerOffersOnly)
        {
            // Safety net; expired regen still forces 1 offer, so Harmony patches do the real work.
            foreach (var range in dynamic.OfferItemCount.Values)
            {
                range.Min = 0;
                range.Max = 0;
            }

            foreach (var traderId in ragfairConfig.Traders.Keys.ToList())
            {
                ragfairConfig.Traders[traderId] = false;
            }

            logger.Info("[UltimateFlea] Player offers only: dynamic/trader flea listings disabled.");
        }
    }
}
