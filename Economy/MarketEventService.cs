using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Common.Models.Logging;
using UltimateFlea.Configs;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class MarketEventService(
    ISptLogger<MarketEventService> logger,
    ConfigManager configManager,
    WipeStage wipeStage)
{
    public double GetMultiplier(MongoId tpl, MongoId parent)
    {
        var cfg = configManager.Events;
        if (!cfg.Enabled || cfg.Events.Count == 0)
        {
            return 1.0;
        }

        var ageDays = wipeStage.GetWipeAgeDays();
        if (ageDays is null)
        {
            return 1.0;
        }

        var mult = 1.0;
        var tplKey = tpl.ToString();
        var parentKey = parent.ToString();
        var activeCount = 0;

        foreach (var ev in cfg.Events)
        {
            if (ageDays.Value < ev.StartDay || ageDays.Value >= ev.EndDay)
            {
                continue;
            }

            if (!Matches(ev, tplKey, parentKey))
            {
                continue;
            }

            mult *= ev.Multiplier;
            activeCount++;
        }

        if (configManager.Mod.Debug && activeCount > 0)
        {
            logger.Debug($"[UltimateFlea] Events for {tplKey}: active={activeCount}, mult={mult:F3}");
        }

        return mult;
    }

    private static bool Matches(MarketEventEntry ev, string tpl, string parent)
    {
        if (ev.Items.Count == 0 && ev.Categories.Count == 0)
        {
            return false;
        }

        foreach (var item in ev.Items)
        {
            if (string.Equals(item, tpl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var category in ev.Categories)
        {
            if (string.Equals(category, parent, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
