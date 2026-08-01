using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using UltimateFlea.Economy;

namespace UltimateFlea.Entry;

// Economy tick on wall-clock, not server update cadence.
[Injectable(TypePriority = OnUpdateOrder.InsuranceCallbacks)]
public class UltimateFleaUpdate(
    ISptLogger<UltimateFleaUpdate> logger,
    ConfigManager configManager,
    EconomyEngine economyEngine) : IOnUpdate
{
    private DateTime _lastTickUtc = DateTime.MinValue;

    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
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
