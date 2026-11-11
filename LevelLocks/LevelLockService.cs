using System.Text.Json;
using UltimateFlea.Util;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace UltimateFlea.LevelLocks;

// Single source of truth: rules are merged into SPT tieredFlea (so the flea UI shows locked offers),
// and the same tables drive the buy/sell guards and the client rules endpoint.
[Injectable(InjectionType.Singleton)]
public class LevelLockService(
    ISptLogger<LevelLockService> logger,
    ConfigManager configManager,
    RagfairConfig ragfairConfig,
    ItemHelper itemHelper,
    TemplateTable templates,
    LocaleTable locales)
{
    public const string LockedLocaleKey = "ultimateflea/LevelLocked{0}";
    public const string ShortLocaleKey = "ultimateflea/LevelShort{0}";

    // One definition for the locked sentence: the locale entry and the server-side error reuse it.
    private const string LockedTemplateEn = "Trading items of this type will be available from level {0}";
    private const string LockedTemplateRu = "Торговля предметами данного типа будет доступна с {0} уровня";
    private const string ShortTemplateEn = "LVL {0}";
    private const string ShortTemplateRu = "{0} ур.";

    // Lock tables are rebuilt from a web request while patches read them, so they are never mutated
    // in place: a fresh immutable snapshot is published with a single atomic reference swap.
    private LockTables _tables = LockTables.Empty;

    public bool Enabled => configManager.Mod.Enabled && configManager.Levels.Enabled;
    public bool LockBuying => Enabled && configManager.Levels.LockBuying;
    public bool LockSelling => Enabled && configManager.Levels.LockSelling;

    public void RegisterLocales()
    {
        if (!Enabled)
        {
            return;
        }

        foreach (var (language, locale) in locales.Global)
        {
            var ru = language.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
            var locked = ru ? LockedTemplateRu : LockedTemplateEn;
            var shortLabel = ru ? ShortTemplateRu : ShortTemplateEn;

            locale.AddTransformer(dict =>
            {
                if (dict is not null)
                {
                    dict[LockedLocaleKey] = locked;
                    dict[ShortLocaleKey] = shortLabel;
                }

                return dict;
            });
        }
    }

    public void Apply()
    {
        if (!Enabled)
        {
            Volatile.Write(ref _tables, LockTables.Empty);
            return;
        }

        var cfg = configManager.Levels;
        var tiered = ragfairConfig.TieredFlea;

        var items = new Dictionary<MongoId, int>();
        var types = new Dictionary<MongoId, int>();
        var ammo = new Dictionary<MongoId, int>();

        SeedBaseTable(cfg.BaseTable, items, types, ammo, ragfairConfig);

        Merge(items, cfg.Items, "items");
        Merge(types, cfg.Categories, "categories");

        foreach (var tpl in items.Keys)
        {
            ammo.Remove(tpl);
        }

        tiered.UnlocksTpl = items;
        tiered.UnlocksType = types;
        tiered.AmmoTplUnlocks = ammo;
        tiered.Enabled = cfg.LockBuying;

        var levels = new Dictionary<MongoId, int>();
        var parentCache = new Dictionary<MongoId, ParentResolution>();

        foreach (var (tpl, template) in templates.Items)
        {
            var level = Resolve(tpl, template.Parent, items, types, ammo, parentCache);
            if (level > 0)
            {
                levels[tpl] = level;
            }
        }

        var categoryLevels = BuildCategoryLevels(levels);

        // Single publish point: readers either see the old snapshot or the complete new one.
        Volatile.Write(ref _tables, new LockTables(levels, categoryLevels));

        logger.Success(
            $"[UltimateFlea] Level locks applied: {levels.Count} items, {categoryLevels.Count} handbook categories locked " +
            $"(buy={cfg.LockBuying}, sell={cfg.LockSelling}).");
    }

    // A handbook category is locked only when every flea-sellable item inside it is locked; shows the lowest level.
    private Dictionary<MongoId, int> BuildCategoryLevels(Dictionary<MongoId, int> levels)
    {
        var handbook = templates.Handbook;
        var parents = handbook.Categories.ToDictionary(c => c.Id, c => c.ParentId);
        var chains = new Dictionary<MongoId, List<MongoId>>();
        var lowest = new Dictionary<MongoId, int>();
        var open = new HashSet<MongoId>();

        foreach (var entry in handbook.Items)
        {
            if (!templates.Items.TryGetValue(entry.Id, out var template) || template.Properties?.CanSellOnRagfair != true)
            {
                continue;
            }

            var level = levels.GetValueOrDefault(entry.Id);
            MongoId? category = entry.ParentId;
            if (category is null)
            {
                continue;
            }

            // Each category chain is walked once and reused by every item inside it.
            foreach (var id in GetChain(category.Value))
            {
                if (level <= 0)
                {
                    open.Add(id);
                }
                else
                {
                    lowest[id] = lowest.TryGetValue(id, out var current) ? Math.Min(current, level) : level;
                }
            }
        }

        return lowest
            .Where(x => !open.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);

        // Same walk as before (including the 32 level depth guard), memoised per starting category.
        List<MongoId> GetChain(MongoId start)
        {
            if (chains.TryGetValue(start, out var cached))
            {
                return cached;
            }

            var chain = new List<MongoId>();
            MongoId? current = start;
            for (var depth = 0; current is not null && depth < 32; depth++)
            {
                var id = current.Value;
                chain.Add(id);
                current = parents.GetValueOrDefault(id);
            }

            chains[start] = chain;
            return chain;
        }
    }

    public int GetRequiredLevel(MongoId tpl)
    {
        return Volatile.Read(ref _tables).Levels.GetValueOrDefault(tpl);
    }

    public bool IsLocked(MongoId tpl, int playerLevel, out int requiredLevel)
    {
        requiredLevel = GetRequiredLevel(tpl);
        return playerLevel < requiredLevel;
    }

    public LevelLocksRules GetRules()
    {
        // One reference for the whole payload, so levels and categories always come from the same snapshot.
        var tables = Volatile.Read(ref _tables);

        return new LevelLocksRules
        {
            Enabled = Enabled,
            LockBuying = LockBuying,
            LockSelling = LockSelling,
            Levels = tables.Levels.ToDictionary(x => x.Key.ToString(), x => x.Value),
            Categories = tables.Categories.ToDictionary(x => x.Key.ToString(), x => x.Value)
        };
    }

    public static string LockedMessage(int requiredLevel)
    {
        return string.Format(LockedTemplateEn, requiredLevel);
    }

    // Same priority as SPT tieredFlea: ammo tier -> exact item -> highest matching category.
    private int Resolve(
        MongoId tpl,
        MongoId parent,
        Dictionary<MongoId, int> items,
        Dictionary<MongoId, int> types,
        Dictionary<MongoId, int> ammo,
        Dictionary<MongoId, ParentResolution> parentCache)
    {
        var resolution = GetParentResolution(tpl, parent, types, ammo.Count > 0, parentCache);

        if (resolution.IsAmmo && ammo.TryGetValue(tpl, out var ammoLevel))
        {
            return ammoLevel;
        }

        if (items.TryGetValue(tpl, out var itemLevel))
        {
            return itemLevel;
        }

        return resolution.CategoryLevel;
    }

    // SPT's base class cache is keyed by item tpl and holds the ids of its parents only, so every item
    // sharing a parent answers IsOfBaseclass identically -> test each parent once.
    // An item whose own parent is not reported (node entries are not in that cache) is resolved
    // directly and never cached, to keep the result bit-for-bit the same as the uncached version.
    private ParentResolution GetParentResolution(
        MongoId tpl,
        MongoId parent,
        Dictionary<MongoId, int> types,
        bool checkAmmo,
        Dictionary<MongoId, ParentResolution> cache)
    {
        var cacheable = !parent.IsEmpty && itemHelper.IsOfBaseclass(tpl, parent);
        if (cacheable && cache.TryGetValue(parent, out var cached))
        {
            return cached;
        }

        var best = 0;
        foreach (var (type, level) in types)
        {
            if (level > best && itemHelper.IsOfBaseclass(tpl, type))
            {
                best = level;
            }
        }

        var resolution = new ParentResolution(
            checkAmmo && itemHelper.IsOfBaseclass(tpl, BaseClasses.AMMO),
            best);

        if (cacheable)
        {
            cache[parent] = resolution;
        }

        return resolution;
    }

    // Seeds the chosen base table: bundled -> spt -> none. A missing/malformed bundled file only warns.
    private void SeedBaseTable(
        string? baseTable,
        Dictionary<MongoId, int> items,
        Dictionary<MongoId, int> types,
        Dictionary<MongoId, int> ammo,
        RagfairConfig ragfair)
    {
        var table = string.IsNullOrEmpty(baseTable) ? "bundled" : baseTable;

        if (table == "bundled")
        {
            if (TryLoadBundled(items, types))
            {
                return;
            }

            logger.Warning("[UltimateFlea] Level locks: bundled table unavailable, falling back to 'spt'.");
            table = "spt";
        }

        var tiered = ragfair.TieredFlea;

        if (table == "spt")
        {
            try
            {
                foreach (var (k, v) in tiered.UnlocksTpl) items[k] = v;
                foreach (var (k, v) in tiered.UnlocksType) types[k] = v;
                if (tiered.AmmoTiersEnabled && tiered.AmmoTplUnlocks is not null)
                {
                    foreach (var (k, v) in tiered.AmmoTplUnlocks) ammo[k] = v;
                }

                return;
            }
            catch (Exception ex)
            {
                logger.Warning($"[UltimateFlea] Level locks: SPT tieredFlea table unusable ({ex.Message}), falling back to 'none'.");
                items.Clear();
                types.Clear();
                ammo.Clear();
            }
        }
        // "none": leave everything empty.
    }

    private bool TryLoadBundled(Dictionary<MongoId, int> items, Dictionary<MongoId, int> types)
    {
        try
        {
            var path = Path.Combine(configManager.ModPath, "config", "levels_bundled.json");
            if (!File.Exists(path))
            {
                logger.Warning($"[UltimateFlea] Level locks: bundled table not found at {path}");
                return false;
            }

            var table = JsonSerializer.Deserialize<BundledTable>(File.ReadAllText(path), JsonFileStore.Config);
            if (table is null)
            {
                logger.Warning("[UltimateFlea] Level locks: bundled table is empty.");
                return false;
            }

            var tmpItems = new Dictionary<MongoId, int>();
            var tmpTypes = new Dictionary<MongoId, int>();
            Merge(tmpItems, table.Items ?? new(), "bundled items");
            Merge(tmpTypes, table.Categories ?? new(), "bundled categories");

            foreach (var (k, v) in tmpItems) items[k] = v;
            foreach (var (k, v) in tmpTypes) types[k] = v;
            return true;
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Level locks: bundled table is malformed ({ex.Message}).");
            return false;
        }
    }

    private sealed class BundledTable
    {
        [JsonPropertyName("items")]
        public Dictionary<string, int>? Items { get; set; }

        [JsonPropertyName("categories")]
        public Dictionary<string, int>? Categories { get; set; }
    }

    private void Merge(Dictionary<MongoId, int> target, Dictionary<string, int> overrides, string section)
    {
        foreach (var (raw, level) in overrides)
        {
            if (!MongoId.IsValidMongoId(raw))
            {
                logger.Warning($"[UltimateFlea] Ignoring invalid ID in levels.json {section}: '{raw}'");
                continue;
            }

            var id = new MongoId(raw);
            if (level <= 0)
            {
                target.Remove(id);
            }
            else
            {
                target[id] = level;
            }
        }
    }

    private readonly record struct ParentResolution(bool IsAmmo, int CategoryLevel);

    // Immutable snapshot of both lock tables, swapped in as a single reference.
    private sealed record LockTables(
        IReadOnlyDictionary<MongoId, int> Levels,
        IReadOnlyDictionary<MongoId, int> Categories)
    {
        public static readonly LockTables Empty = new(
            new Dictionary<MongoId, int>(),
            new Dictionary<MongoId, int>());
    }
}
