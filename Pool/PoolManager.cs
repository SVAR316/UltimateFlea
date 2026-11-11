using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace UltimateFlea.Pool;

// Пул только для flea: CanSellOnRagfair + ragfair custom blacklist.
// ItemConfig.Blacklist не трогаем при remove — иначе пропадает лут PMC/ботов.
// При add чистим глобальный блеклист (WTT), чтобы кастом появился на барахолке.
[Injectable(InjectionType.Singleton)]
public class PoolManager(
    ISptLogger<PoolManager> logger,
    ConfigManager configManager,
    TemplateTable templates,
    RagfairConfig ragfairConfig,
    ItemBlacklistCleaner itemBlacklistCleaner)
{
    public void Apply()
    {
        var poolConfig = configManager.Pool;
        var items = templates.Items;

        var addTpls = ToMongoIdSet(poolConfig.Add);
        var removeTpls = ToMongoIdSet(poolConfig.Remove);
        var userRemoveTpls = removeTpls.ToHashSet();
        var addCats = ToMongoIdSet(poolConfig.AddCategories);
        var removeCats = ToMongoIdSet(poolConfig.RemoveCategories);

        var liveApplied = 0;
        if (poolConfig.UseLiveBlacklist)
        {
            foreach (var tpl in LoadLiveBlacklist())
            {
                // Exact-TPL add keeps precedence (Decide checks add first).
                if (removeTpls.Add(tpl) && !addTpls.Contains(tpl))
                {
                    liveApplied++;
                }
            }

            logger.Info($"[UltimateFlea] Pool: live blacklist (noFlea) applied, {liveApplied} entries added to remove (explicit 'add' entries still win).");
        }

        var whitelistMode = string.Equals(poolConfig.Mode, "whitelist", StringComparison.OrdinalIgnoreCase);

        if (whitelistMode && addCats.Count == 0)
        {
            logger.Warning(
                "[UltimateFlea] Pool: whitelist mode with an empty addCategories removes every item from the flea " +
                "except the explicit 'add' list. Fill addCategories or set mode to 'blacklist'.");
        }

        // Per-Apply cache: resolved (add, remove) category match for each distinct item parent.
        var nested = poolConfig.NestedCategories;
        var catCache = new Dictionary<MongoId, (bool Add, bool Remove)>();
        var anyCats = addCats.Count > 0 || removeCats.Count > 0;

        var enabled = 0;
        var disabled = 0;
        var toUnblacklist = new List<MongoId>();

        // Rebuild the ragfair custom blacklist through a set: calling Remove/Add per item over the
        // whole item DB is O(n^2) while Custom is a list.
        var customBlacklist = ragfairConfig.Dynamic.Blacklist.Custom;
        var custom = customBlacklist.ToHashSet();

        foreach (var (tpl, item) in items)
        {
            if (item.Properties is null)
            {
                continue;
            }

            var catAdd = false;
            var catRemove = false;
            if (anyCats)
            {
                var parent = item.Parent;
                if (!catCache.TryGetValue(parent, out var match))
                {
                    match = ResolveCategories(parent, nested, items, addCats, removeCats);
                    catCache[parent] = match;
                }

                (catAdd, catRemove) = match;
            }

            bool? decision = Decide(tpl, whitelistMode, addTpls, removeTpls, catAdd, catRemove);
            if (decision is null)
            {
                continue;
            }

            if (decision.Value)
            {
                item.Properties.CanSellOnRagfair = true;
                custom.Remove(tpl);
                toUnblacklist.Add(tpl);
                enabled++;
            }
            else
            {
                item.Properties.CanSellOnRagfair = false;
                custom.Add(tpl);
                disabled++;
            }
        }

        customBlacklist.Clear();
        foreach (var tpl in custom)
        {
            customBlacklist.Add(tpl);
        }

        var clearedItemBlacklist = itemBlacklistCleaner.Unblacklist(toUnblacklist);

        var missing = addTpls.Concat(userRemoveTpls)
            .Where(tpl => !items.ContainsKey(tpl))
            .Select(tpl => tpl.ToString())
            .Distinct()
            .ToList();

        if (missing.Count > 0)
        {
            logger.Warning($"[UltimateFlea] Pool: {missing.Count} TPL(s) not found in item DB yet: {string.Join(", ", missing)}");
        }

        // A mistyped category id used to be silently ignored - warn for it like we do for TPLs.
        var knownParents = items.Values.Select(item => item.Parent).ToHashSet();

        var missingCats = addCats.Concat(removeCats)
            .Where(id => !knownParents.Contains(id) && !(poolConfig.NestedCategories && items.ContainsKey(id)))
            .Select(id => id.ToString())
            .Distinct()
            .ToList();

        if (missingCats.Count > 0)
        {
            logger.Warning(
                $"[UltimateFlea] Pool: {missingCats.Count} category ID(s) match no item in the DB: {string.Join(", ", missingCats)}");
        }

        logger.Success(
            $"[UltimateFlea] Pool applied (mode={poolConfig.Mode}): +{enabled} enabled, -{disabled} removed" +
            (clearedItemBlacklist > 0 ? $", cleared {clearedItemBlacklist} from item blacklist" : string.Empty));
    }

    // Does the parent itself, or (nested mode) any ancestor up the _parent chain, match a category rule.
    // Depth capped at 32 like LevelLockService.BuildCategoryLevels; results are cached per parent by the caller.
    private static (bool Add, bool Remove) ResolveCategories(
        MongoId parent,
        bool nested,
        IReadOnlyDictionary<MongoId, TemplateItem> items,
        HashSet<MongoId> addCats,
        HashSet<MongoId> removeCats)
    {
        var add = false;
        var remove = false;
        var current = parent;
        for (var depth = 0; depth < 32 && !current.IsEmpty; depth++)
        {
            add |= addCats.Contains(current);
            remove |= removeCats.Contains(current);
            if (!nested || (add && remove) || !items.TryGetValue(current, out var node))
            {
                break;
            }

            current = node.Parent;
        }

        return (add, remove);
    }

    // true = в пул, false = убрать, null = не трогать. Конкретный TPL бьёт категорию.
    private static bool? Decide(
        MongoId tpl,
        bool whitelistMode,
        HashSet<MongoId> addTpls,
        HashSet<MongoId> removeTpls,
        bool catAdd,
        bool catRemove)
    {
        if (addTpls.Contains(tpl)) return true;
        if (removeTpls.Contains(tpl)) return false;

        if (whitelistMode)
        {
            return catAdd;
        }

        if (catRemove) return false;
        if (catAdd) return true;
        return null;
    }

    private List<MongoId> LoadLiveBlacklist()
    {
        var result = new List<MongoId>();
        try
        {
            var path = System.IO.Path.Combine(configManager.ModPath, "config", "pool_bundled.json");
            if (!File.Exists(path))
            {
                logger.Warning($"[UltimateFlea] Pool: useLiveBlacklist is on but {path} was not found.");
                return result;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("noFlea", out var arr) &&
                arr.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && MongoId.IsValidMongoId(s))
                    {
                        result.Add(new MongoId(s));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"[UltimateFlea] Pool: live blacklist file is malformed ({ex.Message}).");
        }

        return result;
    }

    private HashSet<MongoId> ToMongoIdSet(IEnumerable<string> raw)
    {
        var set = new HashSet<MongoId>();
        foreach (var s in raw)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                continue;
            }

            if (!MongoId.IsValidMongoId(s))
            {
                logger.Warning($"[UltimateFlea] Ignoring invalid TPL in pool config: '{s}' (get IDs from db.sp-tushonka.com)");
                continue;
            }

            set.Add(new MongoId(s));
        }

        return set;
    }
}
