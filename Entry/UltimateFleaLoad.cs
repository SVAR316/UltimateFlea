using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using UltimateFlea.Economy;
using UltimateFlea.LevelLocks;
using UltimateFlea.Pool;
using UltimateFlea.Pricing;

namespace UltimateFlea.Entry;

// Early: turn off handbook regen before flea prices settle.
[Injectable(TypePriority = OnLoadOrder.HandbookCallbacks - 1)]
public class UltimateFleaEarlyLoad(
    ISptLogger<UltimateFleaEarlyLoad> logger,
    ConfigManager configManager,
    RagfairSettingsPatcher ragfairSettingsPatcher,
    LevelLockService levelLocks) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        configManager.Load();

        if (!configManager.Mod.Enabled)
        {
            logger.Info("[UltimateFlea] Disabled via config, skipping.");
            return Task.CompletedTask;
        }

        ragfairSettingsPatcher.Apply();
        levelLocks.RegisterLocales();

        return Task.CompletedTask;
    }
}

// After WTT etc., but before offer generation.
[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class UltimateFleaLoad(
    ISptLogger<UltimateFleaLoad> logger,
    ConfigManager configManager,
    PoolManager poolManager,
    EconomyEngine economyEngine,
    LevelLockService levelLocks) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            configManager.Load();
        }

        if (!configManager.Mod.Enabled)
        {
            return Task.CompletedTask;
        }

        if (configManager.Mod.EnablePoolManager)
        {
            poolManager.Apply();
        }

        if (configManager.Mod.EnableEconomy || configManager.Mod.EnablePricing)
        {
            economyEngine.Initialize();
        }

        levelLocks.Apply();

        logger.Success("[UltimateFlea] Loaded.");
        return Task.CompletedTask;
    }
}
