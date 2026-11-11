using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services.Ragfair;
using SPTarkov.Server.Core.Utils;
using UltimateFlea.Configs;
using UltimateFlea.Pricing;

namespace UltimateFlea.Patches;

// offerSpread = "live": every plain dynamic offer is randomised inside that item's own 24h low/high
// band instead of SPT's one global PriceRanges band, and the final price is clamped to the live
// absolute 24h [low, high] in roubles.
//
// Why not RandomiseOfferPrice: it is a tiny protected method called once from GetDynamicItemPrice, so the
// JIT inlines it and a Harmony detour on it never runs (the in-game offers moved in whole-percent steps,
// i.e. SPT's own Math.Round'ed biased random). GetDynamicItemPrice (large) and
// RandomUtil.GetBiasedRandomNumber (loop, logging) cannot be inlined, so:
//   RagfairPriceService.GetDynamicItemPrice  prefix  -> opens a per-thread scope (tpl, band) for plain offers only
//                                            postfix -> clamps the result into the absolute 24h bounds
//   RandomUtil.GetBiasedRandomNumber         prefix  -> inside a scope, when called with the Default price range,
//                                                       returns our multiplier (x100) instead of SPT's draw
// Weapon presets and pack offers never open a scope, so they keep SPT's own ranges.
// Fail-open: when a method cannot be bound, or anything throws, SPT's own randomisation runs.
[Injectable(TypePriority = OnLoadOrder.Preload)]
public class OfferSpreadPatchesLoad(
    ISptLogger<OfferSpreadPatchesLoad> logger,
    ConfigManager configManager,
    PriceSourceRouter priceRouter,
    RagfairConfig ragfairConfig,
    HandbookHelper handbookHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            configManager.Load();
        }

        OfferSpreadGate.Bind(logger, configManager, priceRouter, ragfairConfig, handbookHelper);

        if (OfferSpreadGate.PriceMethod is null || OfferSpreadGate.RandomMethod is null)
        {
            logger.Warning(
                "[UltimateFlea] offerSpread=live: RagfairPriceService.GetDynamicItemPrice / RandomUtil.GetBiasedRandomNumber not found " +
                "(SPT changed?), per-item offer bands are off and SPT's own price range applies.");
            return Task.CompletedTask;
        }

        try
        {
            new DynamicItemPriceScopePatch().Enable();
            new BiasedRandomPatch().Enable();
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Could not bind the offer spread patches ({ex.Message}); SPT's own price range applies.");
        }

        return Task.CompletedTask;
    }
}

public class DynamicItemPriceScopePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() => OfferSpreadGate.PriceMethod!;

    [PatchPrefix]
    public static void Prefix(MongoId itemTemplateId, Item? item, bool? isPackOffer) =>
        OfferSpreadGate.OpenScope(itemTemplateId, item, isPackOffer);

    [PatchPostfix]
    public static void Postfix(MongoId desiredCurrency, ref double? __result) =>
        OfferSpreadGate.CloseScope(desiredCurrency, ref __result);
}

public class BiasedRandomPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() => OfferSpreadGate.RandomMethod!;

    [PatchPrefix]
    public static bool Prefix(double min, double max, double shift, double n, ref double __result) =>
        !OfferSpreadGate.TryRandomise(min, max, shift, n, out __result);
}

internal static class OfferSpreadGate
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static ISptLogger<OfferSpreadPatchesLoad>? _logger;
    private static ConfigManager? _config;
    private static PriceSourceRouter? _router;
    private static RagfairConfig? _ragfair;
    private static HandbookHelper? _handbook;
    private static int _failedWarned;
    private static int _appliedLogged;
    private static int _mismatchLogged;

    // Per-thread state of the GetDynamicItemPrice call in progress (the method is synchronous).
    [ThreadStatic]
    private static MongoId? _tpl;

    [ThreadStatic]
    private static PriceSpread? _spread;

    [ThreadStatic]
    private static bool _applied;

    public static MethodBase? PriceMethod { get; } = typeof(RagfairPriceService).GetMethod(
        nameof(RagfairPriceService.GetDynamicItemPrice), Flags);

    public static MethodBase? RandomMethod { get; } = typeof(RandomUtil).GetMethod(
        nameof(RandomUtil.GetBiasedRandomNumber), Flags, null,
        [typeof(double), typeof(double), typeof(double), typeof(double)], null);

    public static void Bind(
        ISptLogger<OfferSpreadPatchesLoad> logger,
        ConfigManager config,
        PriceSourceRouter router,
        RagfairConfig ragfair,
        HandbookHelper handbook)
    {
        _logger = logger;
        _config = config;
        _router = router;
        _ragfair = ragfair;
        _handbook = handbook;
    }

    private static void Reset()
    {
        _tpl = null;
        _spread = null;
        _applied = false;
    }

    public static void OpenScope(MongoId tpl, Item? item, bool? isPackOffer)
    {
        try
        {
            Reset();
            if (_config is null || _router is null || _ragfair is null)
            {
                return;
            }

            // Only plain per-item offers: presets (any item carrying a preset id) and packs keep SPT's bands.
            if (isPackOffer == true || item?.Upd?.SptPresetId is not null)
            {
                return;
            }

            var mod = _config.Mod;
            if (!mod.Enabled || !mod.EnablePricing || mod.ResolveOfferSpread() != ModConfig.SpreadLive || !_router.IsLiveActive)
            {
                return;
            }

            if (_router.GetSpread(tpl) is { } spread)
            {
                _tpl = tpl;
                _spread = spread;
            }
        }
        catch (Exception ex)
        {
            Reset();
            Failed(ex);
        }
    }

    // GetBiasedRandomNumber(min, max, shift, n); SPT passes Default.Min/Max * 100 with shift = n = 2.
    // true = result was produced here and SPT's draw must be skipped.
    public static bool TryRandomise(double min, double max, double shift, double n, out double result)
    {
        result = 0;
        try
        {
            if (_spread is not { } spread || _tpl is not { } tpl || _applied || _ragfair is null)
            {
                return false;
            }

            var range = _ragfair.Dynamic.PriceRanges.Default;
            if (shift != 2.0 || n != 2.0 || Math.Abs(min - range.Min * 100.0) > 1e-6 || Math.Abs(max - range.Max * 100.0) > 1e-6)
            {
                if (Interlocked.Exchange(ref _mismatchLogged, 1) == 0)
                {
                    _logger?.Warning(
                        $"[UltimateFlea] Offer spread: unexpected random range inside the price scope (min={min}, max={max}, shift={shift}, n={n}); SPT's draw is used.");
                }

                return false;
            }

            var multiplier = spread.Sample(Random.Shared.NextDouble());
            _applied = true;
            result = multiplier * 100.0;

            if (Interlocked.Exchange(ref _appliedLogged, 1) == 0)
            {
                _logger?.Info(
                    $"[UltimateFlea] Offer spread: per-item band is active (first use: tpl {tpl}, band {spread.Low:0.####}..{spread.High:0.####} " +
                    $"of reference, 24h {spread.AbsLow:0}..{spread.AbsHigh:0} RUB, multiplier {multiplier:0.####}).");
            }

            return true;
        }
        catch (Exception ex)
        {
            Failed(ex);
            return false;
        }
    }

    public static void CloseScope(MongoId desiredCurrency, ref double? result)
    {
        try
        {
            if (_applied && _spread is { } spread && result is { } price && _handbook is not null)
            {
                // The bounds are roubles; the price is in desiredCurrency.
                var low = _handbook.FromRoubles(spread.AbsLow, desiredCurrency);
                var high = _handbook.FromRoubles(spread.AbsHigh, desiredCurrency);
                if (low > 0 && high >= low)
                {
                    result = Math.Clamp(price, low, high);
                }
            }
        }
        catch (Exception ex)
        {
            Failed(ex);
        }
        finally
        {
            Reset();
        }
    }

    private static void Failed(Exception ex)
    {
        if (Interlocked.Exchange(ref _failedWarned, 1) == 0)
        {
            _logger?.Warning($"[UltimateFlea] Per-item offer spread failed ({ex.Message}); using SPT's price range instead.");
        }
    }
}
