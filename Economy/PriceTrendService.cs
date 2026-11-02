using SPTarkov.DI.Annotations;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class PriceTrendService(
    ISptLogger<PriceTrendService> logger,
    ConfigManager configManager)
{
    /// <summary>
    /// Resolves every configured category trend for the given wipe age once per tick,
    /// instead of re-scanning the trend list for each item.
    /// </summary>
    public TrendMultiplierTable BuildTable(double ageDays)
    {
        var cfg = configManager.Trends;
        if (!cfg.Enabled || cfg.Trends.Count == 0)
        {
            return TrendMultiplierTable.Neutral;
        }

        var byCategory = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var trend in cfg.Trends)
        {
            if (string.IsNullOrWhiteSpace(trend.Category) || trend.PeriodDays <= 0)
            {
                continue;
            }

            var angle = 2.0 * Math.PI * (ageDays / trend.PeriodDays) + trend.Phase;
            var mult = 1.0 + trend.Amplitude * Math.Sin(angle);

            byCategory[trend.Category] = byCategory.TryGetValue(trend.Category, out var cur)
                ? cur * mult
                : mult;
        }

        if (configManager.Mod.Debug && byCategory.Count > 0)
        {
            logger.Debug($"[UltimateFlea] Trends resolved for {byCategory.Count} categories at day {ageDays:F2}");
        }

        return byCategory.Count == 0 ? TrendMultiplierTable.Neutral : new TrendMultiplierTable(byCategory);
    }
}

/// <summary>Immutable per-tick view of the category trend multipliers.</summary>
public sealed class TrendMultiplierTable(Dictionary<string, double> byCategory)
{
    public static readonly TrendMultiplierTable Neutral = new([]);

    public double Get(string parentKey) =>
        byCategory.TryGetValue(parentKey, out var mult) ? mult : 1.0;
}
