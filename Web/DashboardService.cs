using SPTarkov.DI.Annotations;
using UltimateFlea.Economy;
using UltimateFlea.LevelLocks;

namespace UltimateFlea.Web;

[Injectable(InjectionType.Singleton)]
public class DashboardService(
    ConfigManager configManager,
    EconomyEngine economyEngine,
    ChartDataBuilder charts,
    LevelLockService levelLocks)
{
    public object GetOverview()
    {
        var snap = economyEngine.GetSnapshot();
        var mod = configManager.Mod;
        var events = configManager.Events;
        var trends = configManager.Trends;
        var levels = configManager.Levels;
        var age = snap.WipeAgeDays;

        var activeEvents = age is null || !events.Enabled
            ? []
            : events.Events
                .Where(e => age.Value >= e.StartDay && age.Value < e.EndDay)
                .Select(e => new
                {
                    id = e.Id,
                    label = e.Id.Replace('_', ' '),
                    e.Multiplier,
                    e.StartDay,
                    e.EndDay,
                })
                .ToList();

        return new
        {
            version = ModMetadata.AssemblyVersion,
            webUi = "/ultimateflea/app.html",
            mod = new
            {
                mod.Enabled,
                mod.EnablePoolManager,
                mod.EnablePricing,
                mod.EnableEconomy,
                mod.PriceSource,
                mod.PlayerOffersOnly,
                mod.QuickListFromStash,
            },
            pool = new
            {
                configManager.Pool.Mode,
                add = configManager.Pool.Add.Count,
                remove = configManager.Pool.Remove.Count,
                addCategories = configManager.Pool.AddCategories.Count,
                removeCategories = configManager.Pool.RemoveCategories.Count,
            },
            economy = new
            {
                snap.ItemCount,
                snap.FixedCount,
                snap.AverageFactor,
                snap.WipeAgeDays,
                snap.WipeMultiplier,
            },
            events = new
            {
                events.Enabled,
                total = events.Events.Count,
                active = activeEvents,
            },
            trends = new
            {
                trends.Enabled,
                total = trends.Trends.Count,
            },
            levels = new
            {
                levels.Enabled,
                levels.UseSptDefaults,
                items = levels.Items.Count,
                categories = levels.Categories.Count,
                rules = levelLocks.GetRules(),
            },
            charts = charts.Build(),
            stateFileExists = File.Exists(Path.Combine(configManager.ModPath, "data", "economy_state.json")),
            // Live count: re-reading and deserializing the whole state file on every
            // overview call was the most expensive part of this endpoint.
            stateEntries = snap.ItemCount,
        };
    }
}
