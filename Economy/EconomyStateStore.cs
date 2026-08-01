using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Economy;

[Injectable(InjectionType.Singleton)]
public class EconomyStateStore(
    ISptLogger<EconomyStateStore> logger,
    ConfigManager configManager)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private string DataDir => Path.Combine(configManager.ModPath, "data");
    private string StatePath => Path.Combine(DataDir, "economy_state.json");

    public Dictionary<string, ItemMarketState> Load()
    {
        try
        {
            if (!File.Exists(StatePath))
            {
                return new Dictionary<string, ItemMarketState>();
            }

            var json = File.ReadAllText(StatePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, ItemMarketState>>(json, JsonOptions);
            return data ?? new Dictionary<string, ItemMarketState>();
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Failed to read economy state, starting fresh. ({ex.Message})");
            return new Dictionary<string, ItemMarketState>();
        }
    }

    public void Save(Dictionary<string, ItemMarketState> state)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(StatePath, json);
        }
        catch (Exception ex)
        {
            logger.Error($"[UltimateFlea] Failed to save economy state: {ex.Message}");
        }
    }
}
