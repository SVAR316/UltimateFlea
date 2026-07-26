using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using UltimateFlea.Economy;
using UltimateFlea.Pool;
using UltimateFlea.Pricing;

namespace UltimateFlea.Entry;

// Раньше handbook regen — выключаем перезапись цен.
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class UltimateFleaEarlyLoad(
    ISptLogger<UltimateFleaEarlyLoad> logger,
    ConfigManager configManager,
    RagfairSettingsPatcher ragfairSettingsPatcher) : IOnLoad
{
    public Task OnLoad()
    {
        configManager.Load();

        if (!configManager.Mod.Enabled)
        {
            logger.Info("[UltimateFlea] Disabled via config, skipping.");
            return Task.CompletedTask;
        }

        if (configManager.Mod.EnableEconomy || configManager.Mod.EnablePricing)
        {
            ragfairSettingsPatcher.Apply();
        }

        return Task.CompletedTask;
    }
}

// После WTT и т.п., но до генерации офферов.
[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class UltimateFleaLoad(
    ISptLogger<UltimateFleaLoad> logger,
    ConfigManager configManager,
    PoolManager poolManager,
    EconomyEngine economyEngine) : IOnLoad
{
    public Task OnLoad()
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

        logger.Success("[UltimateFlea] Loaded.");
        return Task.CompletedTask;
    }
}
