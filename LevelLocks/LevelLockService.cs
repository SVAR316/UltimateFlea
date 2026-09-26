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

    private Dictionary<MongoId, int> _levels = new();
    private Dictionary<MongoId, int> _categoryLevels = new();

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
            var locked = ru
                ? "Торговля предметами данного типа будет доступна с {0} уровня"
                : "Trading items of this type will be available from level {0}";
            var shortLabel = ru ? "{0} ур." : "LVL {0}";

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
        _levels = new Dictionary<MongoId, int>();
        _categoryLevels = new Dictionary<MongoId, int>();

        if (!Enabled)
        {
            return;
        }

        var cfg = configManager.Levels;
        var tiered = ragfairConfig.TieredFlea;

        var items = cfg.UseSptDefaults ? new Dictionary<MongoId, int>(tiered.UnlocksTpl) : new Dictionary<MongoId, int>();
        var types = cfg.UseSptDefaults ? new Dictionary<MongoId, int>(tiered.UnlocksType) : new Dictionary<MongoId, int>();
        var ammo = cfg.UseSptDefaults && tiered.AmmoTiersEnabled && tiered.AmmoTplUnlocks is not null
            ? new Dictionary<MongoId, int>(tiered.AmmoTplUnlocks)
            : new Dictionary<MongoId, int>();

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

        foreach (var tpl in templates.Items.Keys)
        {
            var level = Resolve(tpl, items, types, ammo);
            if (level > 0)
            {
                _levels[tpl] = level;
            }
        }

        BuildCategoryLevels();

        logger.Success(
            $"[UltimateFlea] Level locks applied: {_levels.Count} items, {_categoryLevels.Count} handbook categories locked " +
            $"(buy={cfg.LockBuying}, sell={cfg.LockSelling}).");
    }

    // A handbook category is locked only when every flea-sellable item inside it is locked; shows the lowest level.
    private void BuildCategoryLevels()
    {
        var handbook = templates.Handbook;
        var parents = handbook.Categories.ToDictionary(c => c.Id, c => c.ParentId);
        var lowest = new Dictionary<MongoId, int>();
        var open = new HashSet<MongoId>();

        foreach (var entry in handbook.Items)
        {
            if (!templates.Items.TryGetValue(entry.Id, out var template) || template.Properties?.CanSellOnRagfair != true)
            {
                continue;
            }

            var level = GetRequiredLevel(entry.Id);
            MongoId? category = entry.ParentId;
            for (var depth = 0; category is not null && depth < 32; depth++)
            {
                var id = category.Value;
                if (level <= 0)
                {
                    open.Add(id);
                }
                else
                {
                    lowest[id] = lowest.TryGetValue(id, out var current) ? Math.Min(current, level) : level;
                }

                category = parents.GetValueOrDefault(id);
            }
        }

        _categoryLevels = lowest
            .Where(x => !open.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);
    }

    public int GetRequiredLevel(MongoId tpl)
    {
        return _levels.GetValueOrDefault(tpl);
    }

    public bool IsLocked(MongoId tpl, int playerLevel, out int requiredLevel)
    {
        requiredLevel = GetRequiredLevel(tpl);
        return playerLevel < requiredLevel;
    }

    public LevelLocksRules GetRules()
    {
        return new LevelLocksRules
        {
            Enabled = Enabled,
            LockBuying = LockBuying,
            LockSelling = LockSelling,
            Levels = _levels.ToDictionary(x => x.Key.ToString(), x => x.Value),
            Categories = _categoryLevels.ToDictionary(x => x.Key.ToString(), x => x.Value)
        };
    }

    public static string LockedMessage(int requiredLevel)
    {
        return $"Trading items of this type will be available from level {requiredLevel}";
    }

    // Same priority as SPT tieredFlea: ammo tier -> exact item -> highest matching category.
    private int Resolve(
        MongoId tpl,
        Dictionary<MongoId, int> items,
        Dictionary<MongoId, int> types,
        Dictionary<MongoId, int> ammo)
    {
        if (ammo.Count > 0 && itemHelper.IsOfBaseclass(tpl, BaseClasses.AMMO) && ammo.TryGetValue(tpl, out var ammoLevel))
        {
            return ammoLevel;
        }

        if (items.TryGetValue(tpl, out var itemLevel))
        {
            return itemLevel;
        }

        var best = 0;
        foreach (var (type, level) in types)
        {
            if (level > best && itemHelper.IsOfBaseclass(tpl, type))
            {
                best = level;
            }
        }

        return best;
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
}
