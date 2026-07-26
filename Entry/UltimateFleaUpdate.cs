using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using UltimateFlea.Economy;

namespace UltimateFlea.Entry;

// Тик экономики по wall-clock, не по апдейт-каденсу сервера.
[Injectable(TypePriority = OnUpdateOrder.InsuranceCallbacks)]
public class UltimateFleaUpdate(
    ISptLogger<UltimateFleaUpdate> logger,
    ConfigManager configManager,
    EconomyEngine economyEngine) : IOnUpdate
{
    private DateTime _lastTickUtc = DateTime.MinValue;

    public Task<bool> OnUpdate(long timeSinceLastRun)
    {
        if (!configManager.Mod.Enabled || !configManager.Mod.EnableEconomy)
        {
            return Task.FromResult(false);
        }

        var interval = TimeSpan.FromMinutes(configManager.Economy.SimIntervalMinutes);
        var now = DateTime.UtcNow;

        if (now - _lastTickUtc < interval)
        {
            return Task.FromResult(false);
        }

        _lastTickUtc = now;
        economyEngine.Tick();

        if (configManager.Mod.Debug)
        {
            logger.Debug("[UltimateFlea] Economy tick complete.");
        }

        return Task.FromResult(true);
    }
}
