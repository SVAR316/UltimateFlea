using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using UltimateFlea.Presets;
using UltimateFlea.Pricing;

namespace UltimateFlea.Economy;

public sealed record ResetRequest(bool PriceCache, bool EconomyState);

public sealed record ResetResult(
    bool Ok,
    List<string> DeletedFiles,
    List<string> BackupFiles,
    int LivePrices,
    int Repriced,
    string? PriceSource,
    LocalizedText? Error);

/// <summary>Dashboard-triggered reset of the live price cache and/or the economy state.</summary>
[Injectable(InjectionType.Singleton)]
public class ResetService(
    ISptLogger<ResetService> logger,
    ConfigManager configManager,
    PriceSourceRouter priceSource,
    EconomyEngine economyEngine,
    EconomyStateStore stateStore)
{
    // One reset at a time; a second click while the (slow) refetch runs is rejected.
    private readonly SemaphoreSlim _busy = new(1, 1);

    public ResetResult Run(ResetRequest req)
    {
        var deleted = new List<string>();
        var backups = new List<string>();

        if (!_busy.Wait(0))
        {
            return Fail(deleted, backups, "A reset is already running.", "Сброс уже выполняется.");
        }

        try
        {
            if (!configManager.Mod.Enabled || !(configManager.Mod.EnableEconomy || configManager.Mod.EnablePricing))
            {
                return Fail(deleted, backups, "Economy and pricing are disabled in mod.json, nothing to reset.",
                    "Экономика и ценообразование отключены в mod.json, сбрасывать нечего.");
            }

            if (string.IsNullOrEmpty(configManager.ModPath))
            {
                return Fail(deleted, backups, "Mod path is not known yet.", "Путь к моду ещё не известен.");
            }

            if (req.PriceCache)
            {
                var errors = new List<string>();
                if (!priceSource.ResetLiveCache(deleted, errors))
                {
                    return Fail(deleted, backups,
                        $"Could not delete the price cache (economy untouched): {string.Join("; ", errors)}",
                        $"Не удалось удалить кэш цен (экономика не тронута): {string.Join("; ", errors)}");
                }

                // Outside the engine gate: this may be a long download.
                priceSource.Refresh();
            }

            int repriced;
            try
            {
                repriced = economyEngine.Initialize(refreshPrices: false, resetState: req.EconomyState);
            }
            catch (Exception ex)
            {
                logger.Error($"[UltimateFlea] Reset failed during re-initialize: {ex.Message}", ex);
                return Fail(deleted, backups, $"Re-initialize failed: {ex.Message}", $"Повторная инициализация не удалась: {ex.Message}");
            }

            if (req.EconomyState)
            {
                foreach (var f in stateStore.LastResetFiles)
                {
                    (f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ? backups : deleted).Add(f);
                }
            }

            var what = string.Join(" + ", new[] { req.PriceCache ? "price cache" : null, req.EconomyState ? "economy state" : null }.Where(x => x is not null));
            logger.Info(
                $"[UltimateFlea] Reset via dashboard: {what}. Deleted {deleted.Count} file(s), backups {backups.Count}, " +
                $"{priceSource.LivePriceCount} live prices, {repriced} items repriced (source={priceSource.Id}).");

            return new ResetResult(true, deleted, backups, priceSource.LivePriceCount, repriced, priceSource.Id, null);
        }
        catch (Exception ex)
        {
            logger.Error($"[UltimateFlea] Reset failed: {ex.Message}", ex);
            return Fail(deleted, backups, $"Reset failed: {ex.Message}", $"Сброс не удался: {ex.Message}");
        }
        finally
        {
            _busy.Release();
        }
    }

    private ResetResult Fail(List<string> deleted, List<string> backups, string en, string ru) =>
        new(false, deleted, backups, priceSource.LivePriceCount, 0, priceSource.Id, new LocalizedText(en, ru));
}
