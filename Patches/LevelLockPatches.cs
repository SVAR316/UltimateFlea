using System.Reflection;
using SPTarkov.Common.Models.Logging;
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

// The buy guard lives in ConfirmRagfairTradingPatch (Patches/TradeHooks.cs): that method is also
// hooked for demand tracking, and a single patch class is the only way to order the two reliably.
[Injectable(TypePriority = OnLoadOrder.Preload)]
public class LevelLockPatchesLoad(
    ISptLogger<LevelLockPatchesLoad> logger,
    LevelLockService levelLocks,
    RagfairOfferService ragfairOfferService,
    HttpResponseUtil httpResponseUtil,
    EventOutputHolder eventOutputHolder) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        LevelLockGate.Bind(logger, levelLocks, ragfairOfferService, httpResponseUtil, eventOutputHolder);
        new LevelLockSellPatch().Enable();
        return Task.CompletedTask;
    }
}

internal static class LevelLockGate
{
    private static ISptLogger<LevelLockPatchesLoad>? _logger;
    private static LevelLockService? _levelLocks;
    private static RagfairOfferService? _offers;
    private static HttpResponseUtil? _http;
    private static EventOutputHolder? _output;
    private static int _unboundWarned;

    public static void Bind(
        ISptLogger<LevelLockPatchesLoad> logger,
        LevelLockService levelLocks,
        RagfairOfferService offers,
        HttpResponseUtil http,
        EventOutputHolder output)
    {
        _logger = logger;
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

        // One index per request instead of a linear scan of the whole stash per requested item.
        // GroupBy keeps the first entry for an id, which is what FirstOrDefault used to return.
        var stashById = pmcData.Inventory.Items
            .GroupBy(i => $"{i.Id}")
            .ToDictionary(g => g.Key, g => g.First());

        var level = pmcData.Info?.Level ?? 0;
        foreach (var itemId in request.Items)
        {
            if (stashById.TryGetValue($"{itemId}", out var item)
                && _levelLocks.IsLocked(item.Template, level, out var required))
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

    // Shared prefix body for both guards: true = let the original controller method run.
    public static bool Allow(int? requiredLevel, MongoId sessionID, ref ItemEventRouterResponse result)
    {
        var blocked = Fail(requiredLevel, sessionID);
        if (blocked is null)
        {
            return true;
        }

        result = blocked;
        return false;
    }

    // Returns null when nothing is locked, or when our statics are not bound yet: an NRE inside a
    // Harmony prefix would take down the whole trade endpoint, so an unbound gate allows the trade.
    public static ItemEventRouterResponse? Fail(int? requiredLevel, MongoId sessionID)
    {
        if (requiredLevel is null)
        {
            return null;
        }

        var output = _output;
        var http = _http;
        if (output is null || http is null)
        {
            if (Interlocked.Exchange(ref _unboundWarned, 1) == 0)
            {
                _logger?.Warning(
                    "[UltimateFlea] Level lock gate is not bound (no output/response util); flea trades are allowed through.");
            }

            return null;
        }

        return http.AppendErrorToOutput(
            output.GetOutput(sessionID),
            LevelLockService.LockedMessage(requiredLevel.Value),
            BackendErrorCodes.RagfairUnavailable);
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
        return LevelLockGate.Allow(LevelLockGate.CheckSell(pmcData, offerRequest), sessionID, ref __result);
    }
}
