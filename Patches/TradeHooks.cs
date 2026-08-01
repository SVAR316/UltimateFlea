using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Eft.Trade;
using SPTarkov.Server.Core.Services.Ragfair;
using UltimateFlea.Economy;

namespace UltimateFlea.Patches;

// Покупка на flea -> demand. Успешная продажа лота игрока -> supply.
[Injectable(TypePriority = OnLoadOrder.Preload)]
public class TradeHooksLoad(
    ISptLogger<TradeHooksLoad> logger,
    ConfigManager configManager,
    EconomyEngine economyEngine,
    RagfairOfferService ragfairOfferService) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        TradeHookHelper.Initialize(configManager, economyEngine, ragfairOfferService);
        new ConfirmRagfairTradingPatch().Enable();
        new CompleteOfferPatch().Enable();
        logger.Info("[UltimateFlea] Trade hooks enabled.");
        return Task.CompletedTask;
    }
}

internal static class TradeHookHelper
{
    private static readonly AsyncLocal<List<MongoId>?> PendingBuys = new();

    private static ConfigManager? _config;
    private static EconomyEngine? _engine;
    private static RagfairOfferService? _offers;

    public static void Initialize(
        ConfigManager configManager,
        EconomyEngine economyEngine,
        RagfairOfferService ragfairOfferService)
    {
        _config = configManager;
        _engine = economyEngine;
        _offers = ragfairOfferService;
    }

    public static void CaptureBuys(ProcessRagfairTradeRequestData? request)
    {
        var tpls = new List<MongoId>();
        PendingBuys.Value = tpls;

        if (request?.Offers is null || request.Offers.Count == 0 || _offers is null)
        {
            return;
        }

        foreach (var offerReq in request.Offers)
        {
            if (string.IsNullOrWhiteSpace(offerReq.Id) || !MongoId.IsValidMongoId(offerReq.Id))
            {
                continue;
            }

            var offer = _offers.GetOfferByOfferId(new MongoId(offerReq.Id));
            var tpl = offer?.Items?.FirstOrDefault()?.Template;
            if (tpl is not null && !tpl.Value.IsEmpty)
            {
                tpls.Add(tpl.Value);
            }
        }
    }

    public static void CommitBuys(ItemEventRouterResponse? result)
    {
        var tpls = PendingBuys.Value;
        PendingBuys.Value = null;

        if (tpls is null || tpls.Count == 0)
        {
            return;
        }

        if (result?.Warnings is { Count: > 0 })
        {
            return;
        }

        var engine = TryGetEngine();
        if (engine is null)
        {
            return;
        }

        foreach (var tpl in tpls)
        {
            engine.RecordBuy(tpl);
        }
    }

    public static void RecordSell(RagfairOffer? offer)
    {
        var tpl = offer?.Items?.FirstOrDefault()?.Template;
        if (tpl is null || tpl.Value.IsEmpty)
        {
            return;
        }

        TryGetEngine()?.RecordSell(tpl.Value);
    }

    private static EconomyEngine? TryGetEngine()
    {
        var config = _config;
        if (config is null || string.IsNullOrEmpty(config.ModPath))
        {
            return null;
        }

        if (!config.Mod.Enabled || !config.Mod.EnableEconomy)
        {
            return null;
        }

        return _engine;
    }
}

public class ConfirmRagfairTradingPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TradeController).GetMethod(
            nameof(TradeController.ConfirmRagfairTrading),
            BindingFlags.Instance | BindingFlags.Public)!;
    }

    [PatchPrefix]
    public static void Prefix(ProcessRagfairTradeRequestData request)
    {
        TradeHookHelper.CaptureBuys(request);
    }

    [PatchPostfix]
    public static void Postfix(ItemEventRouterResponse __result)
    {
        TradeHookHelper.CommitBuys(__result);
    }
}

public class CompleteOfferPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagfairOfferHelper).GetMethod(
            nameof(RagfairOfferHelper.CompleteOffer),
            BindingFlags.Instance | BindingFlags.Public)!;
    }

    [PatchPostfix]
    public static void Postfix(RagfairOffer offer, ItemEventRouterResponse __result)
    {
        if (__result?.Warnings is { Count: > 0 })
        {
            return;
        }

        TradeHookHelper.RecordSell(offer);
    }
}
