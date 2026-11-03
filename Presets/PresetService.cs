using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using UltimateFlea.Util;

namespace UltimateFlea.Presets;

/// <summary>
/// Lists, resolves and applies presets. Applying goes through <see cref="ConfigManager.TrySaveConfig"/>
/// so validation, clamping, atomic writes and publishing are exactly those of a manual save.
/// </summary>
[Injectable(InjectionType.Singleton)]
public class PresetService(
    ISptLogger<PresetService> logger,
    ConfigManager configManager,
    ModdedItemsDetector moddedItems,
    TemplateTable templates)
{
    // Two simultaneous applies would interleave their per-config saves and leave a mix of both presets.
    private readonly object _applyGate = new();

    public IReadOnlyList<PresetSummary> List()
    {
        // Probed once per listing, not per preset.
        ModdedItemsProbe? probe = null;

        return PresetCatalog.All.Select(def =>
        {
            LocalizedText? reason = null;
            if (def.Id == PresetCatalog.ModdedItemsPresetId)
            {
                probe ??= moddedItems.Probe();
                reason = probe.UnavailableReason;
            }

            return new PresetSummary(def.Id, def.Name, def.Description, def.Targets, def.Tags, def.Requires,
                reason is null, reason);
        }).ToList();
    }

    public PresetDefinition? Find(string id) => PresetCatalog.Find(id);

    /// <summary>The exact config bodies applying would write, or the reason the preset cannot be resolved.</summary>
    public bool TryResolve(PresetDefinition def, out IReadOnlyDictionary<string, JsonElement> configs, out LocalizedText? error) =>
        TryResolve(def, out configs, out error, out _);

    private bool TryResolve(PresetDefinition def, out IReadOnlyDictionary<string, JsonElement> configs, out LocalizedText? error, out LocalizedText? summary)
    {
        error = null;
        summary = null;
        configs = new Dictionary<string, JsonElement>();

        if (def.Kind == PresetKind.Static)
        {
            configs = def.Configs!;
            return true;
        }

        var probe = moddedItems.Probe();
        if (probe.UnavailableReason is not null)
        {
            error = probe.UnavailableReason;
            return false;
        }

        // Union with the current pool: the preset must add the mod items, not wipe the user's own lists.
        var current = configManager.Pool;
        var add = current.Add.Concat(probe.Tpls).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var pool = current with { Add = add, Remove = current.Remove.Where(r => !probe.Tpls.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList() };

        summary = ModdedSummary(probe);

        configs = new Dictionary<string, JsonElement>
        {
            ["pool"] = JsonSerializer.SerializeToElement(pool, JsonFileStore.Config),
        };
        return true;
    }

    // Says what actually happened: how many items, from which mods, and how many of unknown source.
    private static LocalizedText ModdedSummary(ModdedItemsProbe probe)
    {
        var mods = string.Join(", ", probe.ByMod.Select(m => $"{m.ModName}: {m.Count}"));
        var en = $"Added {probe.Tpls.Count} item(s) to the flea pool";
        var ru = $"Добавлено предметов в пул барахолки: {probe.Tpls.Count}";

        if (mods.Length > 0)
        {
            en += $" ({mods})";
            ru += $" ({mods})";
        }

        if (probe.UnknownSourceCount > 0)
        {
            en += $", {probe.UnknownSourceCount} of unknown source";
            ru += $", с неизвестным источником: {probe.UnknownSourceCount}";
        }

        return new(en + ".", ru + ".");
    }

    public PresetApplyResult Apply(PresetDefinition def)
    {
        lock (_applyGate)
        {
            if (!TryResolve(def, out var configs, out var resolveError, out var summary))
            {
                return new([], false, null, resolveError, 400);
            }

            var applied = new List<string>();
            var restart = false;

            foreach (var (name, body) in configs)
            {
                if (!configManager.TrySaveConfig(name, body, out var error, out var restartRecommended))
                {
                    logger.Error($"[UltimateFlea] Preset '{def.Id}' stopped at '{name}': {error}");

                    // Earlier configs are already on disk and live; no rollback, so say exactly what happened.
                    // The technical detail after the colon comes from the server and is not translated.
                    var saved = string.Join(", ", applied);
                    LocalizedText message = applied.Count == 0
                        ? new($"Could not apply '{name}': {error}", $"Не удалось применить '{name}': {error}")
                        : new(
                            $"Preset partially applied. Saved: {saved}. Failed on '{name}': {error}. Remaining configs were not changed.",
                            $"Пресет применён частично. Сохранено: {saved}. Ошибка на '{name}': {error}. Остальные настройки не менялись.");
                    return new(applied, restart, null, message, 400);
                }

                applied.Add(name);
                restart |= restartRecommended;
            }

            logger.Success($"[UltimateFlea] Applied preset '{def.Id}' ({string.Join(", ", applied)})");
            return new(applied, restart, LocalizedText.Join(summary, UnknownIdWarning(configs)), null, 200);
        }
    }

    /// <summary>
    /// Ids in a preset that this server's item database does not know are not an error (PoolManager and
    /// EconomyEngine skip them), but the user should hear about it.
    /// </summary>
    private LocalizedText? UnknownIdWarning(IReadOnlyDictionary<string, JsonElement> configs)
    {
        var items = templates.Items;
        var unknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Check(IEnumerable<string> ids)
        {
            foreach (var id in ids)
            {
                if (!MongoId.IsValidMongoId(id) || !items.ContainsKey(new MongoId(id)))
                {
                    unknown.Add(id);
                }
            }
        }

        if (configs.ContainsKey("pool"))
        {
            var pool = configManager.Pool;
            Check(pool.Add);
            Check(pool.Remove);
        }

        if (configs.ContainsKey("pricing"))
        {
            var pricing = configManager.Pricing;
            Check(pricing.FixedPrices.Keys);
            Check(pricing.ItemMultipliers.Keys);
        }

        if (unknown.Count == 0)
        {
            return null;
        }

        var sample = string.Join(", ", unknown.Take(5));
        return new(
            $"{unknown.Count} item id(s) in the preset are not in this server's item database and were ignored (for example {sample}).",
            $"Предметов, которых нет в базе этого сервера и которые были проигнорированы: {unknown.Count} (например {sample}).");
    }
}
