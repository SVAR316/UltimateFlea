using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Eft.Trade;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Ragfair;
using SPTarkov.Server.Core.Utils;
using UltimateFlea.LevelLocks;

namespace UltimateFlea.Patches;

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class LevelLockPatchesLoad(
    LevelLockService levelLocks,
    RagfairOfferService ragfairOfferService,
    HttpResponseUtil httpResponseUtil,
    EventOutputHolder eventOutputHolder) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        LevelLockGate.Bind(levelLocks, ragfairOfferService, httpResponseUtil, eventOutputHolder);
        new LevelLockSellPatch().Enable();
        new LevelLockBuyPatch().Enable();
        return Task.CompletedTask;
    }
}

internal static class LevelLockGate
{
    private static LevelLockService? _levelLocks;
    private static RagfairOfferService? _offers;
    private static HttpResponseUtil? _http;
    private static EventOutputHolder? _output;

    public static void Bind(
        LevelLockService levelLocks,
        RagfairOfferService offers,
        HttpResponseUtil http,
        EventOutputHolder output)
    {
        _levelLocks = levelLocks;
        _offers = offers;
        _http = http;
        _output = output;
    }

    public static int? CheckSell(PmcData? pmcData, AddOfferRequestData? request)
    {
        if (_levelLocks is not { LockSelling: true } || pmcData?.Inventory?.Items is null || request?.Items is null)
        {
            return null;
        }

        var level = pmcData.Info?.Level ?? 0;
        foreach (var itemId in request.Items)
        {
            var item = pmcData.Inventory.Items.FirstOrDefault(i => i.Id == itemId);
            if (item is not null && _levelLocks.IsLocked(item.Template, level, out var required))
            {
                return required;
            }
        }

        return null;
    }

    public static int? CheckBuy(PmcData? pmcData, ProcessRagfairTradeRequestData? request)
    {
        if (_levelLocks is not { LockBuying: true } || _offers is null || pmcData is null || request?.Offers is null)
        {
            return null;
        }

        var level = pmcData.Info?.Level ?? 0;
        foreach (var offerReq in request.Offers)
        {
            if (string.IsNullOrWhiteSpace(offerReq.Id) || !MongoId.IsValidMongoId(offerReq.Id))
            {
                continue;
            }

            var offer = _offers.GetOfferByOfferId(new MongoId(offerReq.Id));
            if (offer is null || offer.IsTraderOffer())
            {
                continue;
            }

            var tpl = offer.Items?.FirstOrDefault()?.Template;
            if (tpl is not null && _levelLocks.IsLocked(tpl.Value, level, out var required))
            {
                return required;
            }
        }

        return null;
    }

    public static ItemEventRouterResponse Fail(MongoId sessionID, int requiredLevel)
    {
        var output = _output!.GetOutput(sessionID);
        return _http!.AppendErrorToOutput(output, LevelLockService.LockedMessage(requiredLevel), BackendErrorCodes.RagfairUnavailable);
    }
}

public class LevelLockSellPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RagfairController).GetMethod(
            nameof(RagfairController.AddPlayerOffer),
            BindingFlags.Instance | BindingFlags.Public)!;
    }

    [PatchPrefix]
    public static bool Prefix(PmcData pmcData, AddOfferRequestData offerRequest, MongoId sessionID, ref ItemEventRouterResponse __result)
    {
        var required = LevelLockGate.CheckSell(pmcData, offerRequest);
        if (required is null)
        {
            return true;
        }

        __result = LevelLockGate.Fail(sessionID, required.Value);
        return false;
    }
}

public class LevelLockBuyPatch : AbstractPatch
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
        var required = LevelLockGate.CheckBuy(pmcData, request);
        if (required is null)
        {
            return true;
        }

        __result = LevelLockGate.Fail(sessionID, required.Value);
        return false;
    }
}
