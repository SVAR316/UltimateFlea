using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
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
    // Captured offers for the request currently being handled on this execution context.
    private static readonly AsyncLocal<PendingBuy?> Pending = new();

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
        DiscardBuys();

        if (request?.Offers is null || request.Offers.Count == 0 || _offers is null)
        {
            return;
        }

        var tpls = new List<MongoId>();
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

        Pending.Value = new PendingBuy(request, tpls);
    }

    // Called whenever the trade is not going to run (blocked by a guard), so nothing can be recorded.
    public static void DiscardBuys()
    {
        Pending.Value = null;
    }

    public static void CommitBuys(ProcessRagfairTradeRequestData? request, ItemEventRouterResponse? result)
    {
        var pending = Pending.Value;
        DiscardBuys();

        // No capture means the prefix never let the trade reach the controller: record nothing.
        // This is the real gate - it does not depend on how a rejection is reported.
        if (pending is null || pending.Tpls.Count == 0)
        {
            return;
        }

        // The capture must belong to the request we are returning from, not to a leftover context.
        if (!ReferenceEquals(pending.Request, request))
        {
            return;
        }

        // Evidence the controller actually produced an item event result; a trade it refused
        // (not enough money, offer gone, ...) comes back with a warning attached.
        if (result is null || result.Warnings is { Count: > 0 })
        {
            return;
        }

        var engine = TryGetEngine();
        if (engine is null)
        {
            return;
        }

        foreach (var tpl in pending.Tpls)
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

    private sealed record PendingBuy(ProcessRagfairTradeRequestData Request, List<MongoId> Tpls);
}

// ConfirmRagfairTrading is guarded by the level locks AND hooked for demand tracking. Both used to
// live in separate patch classes, where Harmony gives no ordering guarantee between the prefixes -
// the capture could run for a trade the level lock had already rejected. One class, one explicit
// order: level lock decides first, and only an allowed trade is ever captured.
public class ConfirmRagfairTradingPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TradeController).GetMethod(
            nameof(TradeController.ConfirmRagfairTrading),
            BindingFlags.Instance | BindingFlags.Public)!;
    }

    [PatchPrefix]
    public static bool Prefix(PmcData pmcData, ProcessRagfairTradeRequestData request, MongoId sessionID, ref ItemEventRouterResponse __result)
    {
        // 1. Level lock. A blocked trade returns before anything is captured.
        if (!LevelLockGate.Allow(LevelLockGate.CheckBuy(pmcData, request), sessionID, ref __result))
        {
            TradeHookHelper.DiscardBuys();
            return false;
        }

        // 2. The trade is allowed to run: remember what is being bought for the postfix.
        TradeHookHelper.CaptureBuys(request);
        return true;
    }

    [PatchPostfix]
    public static void Postfix(ProcessRagfairTradeRequestData request, ItemEventRouterResponse __result)
    {
        TradeHookHelper.CommitBuys(request, __result);
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
