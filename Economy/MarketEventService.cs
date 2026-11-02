using SPTarkov.DI.Annotations;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class MarketEventService(
    ISptLogger<MarketEventService> logger,
    ConfigManager configManager)
{
    /// <summary>
    /// Flattens the configured events into two lookups for the given wipe age.
    /// Built once per tick: the old per-item path scanned every event for every
    /// item and re-resolved the wipe age (a full profile walk) each time.
    /// </summary>
    public EventMultiplierTable BuildTable(double? ageDays)
    {
        var cfg = configManager.Events;
        if (!cfg.Enabled || cfg.Events.Count == 0 || ageDays is null)
        {
            return EventMultiplierTable.Neutral;
        }

        var byItem = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var byCategory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var active = 0;

        foreach (var ev in cfg.Events)
        {
            // startDay inclusive, endDay exclusive.
            if (ageDays.Value < ev.StartDay || ageDays.Value >= ev.EndDay)
            {
                continue;
            }

            if (ev.Items.Count == 0 && ev.Categories.Count == 0)
            {
                continue;
            }

            active++;

            foreach (var item in ev.Items)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                byItem[item] = byItem.TryGetValue(item, out var cur) ? cur * ev.Multiplier : ev.Multiplier;
            }

            foreach (var category in ev.Categories)
            {
                if (string.IsNullOrWhiteSpace(category)) continue;
                byCategory[category] = byCategory.TryGetValue(category, out var cur) ? cur * ev.Multiplier : ev.Multiplier;
            }
        }

        if (configManager.Mod.Debug && active > 0)
        {
            logger.Debug($"[UltimateFlea] Events active at day {ageDays.Value:F2}: {active} " +
                         $"({byItem.Count} item rules, {byCategory.Count} category rules)");
        }

        return active == 0 ? EventMultiplierTable.Neutral : new EventMultiplierTable(byItem, byCategory);
    }
}

/// <summary>Immutable per-tick view of the active event multipliers.</summary>
public sealed class EventMultiplierTable(
    Dictionary<string, double> byItem,
    Dictionary<string, double> byCategory)
{
    public static readonly EventMultiplierTable Neutral = new([], []);

    public double Get(string tplKey, string parentKey)
    {
        var mult = 1.0;
        if (byItem.TryGetValue(tplKey, out var itemMult)) mult *= itemMult;
        if (byCategory.TryGetValue(parentKey, out var catMult)) mult *= catMult;
        return mult;
    }
}
