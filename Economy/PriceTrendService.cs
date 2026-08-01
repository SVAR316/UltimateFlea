using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class PriceTrendService(
    ISptLogger<PriceTrendService> logger,
    ConfigManager configManager,
    WipeStage wipeStage)
{
    public double GetMultiplier(MongoId parent)
    {
        var cfg = configManager.Trends;
        if (!cfg.Enabled || cfg.Trends.Count == 0)
        {
            return 1.0;
        }

        var ageDays = wipeStage.GetWipeAgeDays() ?? 0.0;
        var parentKey = parent.ToString();
        var mult = 1.0;
        var matched = 0;

        foreach (var trend in cfg.Trends)
        {
            if (!string.Equals(trend.Category, parentKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var angle = 2.0 * Math.PI * (ageDays / trend.PeriodDays) + trend.Phase;
            mult *= 1.0 + trend.Amplitude * Math.Sin(angle);
            matched++;
        }

        if (configManager.Mod.Debug && matched > 0)
        {
            logger.Debug($"[UltimateFlea] Trend for category {parentKey}: mult={mult:F3}");
        }

        return mult;
    }
}
