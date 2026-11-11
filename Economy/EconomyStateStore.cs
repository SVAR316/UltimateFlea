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

    /// <summary>Files removed or written by the last <see cref="BackupAndClear"/>.</summary>
    public List<string> LastResetFiles { get; } = new();

    /// <summary>
    /// Keeps the current state as "economy_state.json.bak" (plus a timestamped copy, because the
    /// next save rotates the plain .bak) and deletes the file. An empty or missing file never
    /// overwrites an existing backup. Nothing is deleted unless the backup succeeded.
    /// </summary>
    public bool BackupAndClear(out string error)
    {
        error = string.Empty;
        LastResetFiles.Clear();

        if (string.IsNullOrEmpty(configManager.ModPath))
        {
            error = "Mod path is not known yet.";
            return false;
        }

        try
        {
            var path = StatePath;
            if (!File.Exists(path))
            {
                return true;
            }

            var text = File.ReadAllText(path).Trim();
            if (text.Length > 0 && text != "{}" && text != "null")
            {
                File.Copy(path, path + ".bak", overwrite: true);
                var stamped = $"{path}.reset-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
                File.Copy(path, stamped, overwrite: true);
                LastResetFiles.Add(path + ".bak");
                LastResetFiles.Add(stamped);
            }

            File.Delete(path);
            LastResetFiles.Add(path);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Economy state backup/delete failed: {ex.Message}";
            logger.Error($"[UltimateFlea] {error}", ex);
            return false;
        }
    }
}
