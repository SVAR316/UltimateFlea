using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using UltimateFlea.Util;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class EconomyStateStore(
    ISptLogger<EconomyStateStore> logger,
    ConfigManager configManager)
{
    private string StatePath => Path.Combine(JsonFileStore.DataDir(configManager.ModPath), "economy_state.json");

    public Dictionary<string, ItemMarketState> Load()
    {
        // ModPath is empty until ConfigManager.Load() ran; a relative path would hit the server CWD.
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            logger.Error("[UltimateFlea] Mod path is not known yet, cannot read economy state.");
            return new Dictionary<string, ItemMarketState>();
        }

        return JsonFileStore.ReadOrDefault<Dictionary<string, ItemMarketState>, EconomyStateStore>(
            StatePath,
            JsonFileStore.Data,
            logger,
            "economy state") ?? new Dictionary<string, ItemMarketState>();
    }

    public void Save(Dictionary<string, ItemMarketState> state)
    {
        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            logger.Error("[UltimateFlea] Mod path is not known yet, skipping economy state save.");
            return;
        }

        JsonFileStore.WriteAtomic(StatePath, state, JsonFileStore.Data, logger, "economy state", out _);
    }
}
