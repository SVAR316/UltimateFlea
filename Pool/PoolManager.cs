using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
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
        var addCats = ToMongoIdSet(poolConfig.AddCategories);
        var removeCats = ToMongoIdSet(poolConfig.RemoveCategories);

        var whitelistMode = string.Equals(poolConfig.Mode, "whitelist", StringComparison.OrdinalIgnoreCase);

        if (whitelistMode && addCats.Count == 0)
        {
            logger.Warning(
                "[UltimateFlea] Pool: whitelist mode with an empty addCategories removes every item from the flea " +
                "except the explicit 'add' list. Fill addCategories or set mode to 'blacklist'.");
        }

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

            bool? decision = Decide(tpl, item.Parent, whitelistMode, addTpls, removeTpls, addCats, removeCats);
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

        var missing = addTpls.Concat(removeTpls)
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
            .Where(id => !knownParents.Contains(id))
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

    // true = в пул, false = убрать, null = не трогать. Конкретный TPL бьёт категорию.
    private static bool? Decide(
        MongoId tpl,
        MongoId parent,
        bool whitelistMode,
        HashSet<MongoId> addTpls,
        HashSet<MongoId> removeTpls,
        HashSet<MongoId> addCats,
        HashSet<MongoId> removeCats)
    {
        if (addTpls.Contains(tpl)) return true;
        if (removeTpls.Contains(tpl)) return false;

        var catAdd = addCats.Contains(parent);
        var catRemove = removeCats.Contains(parent);

        if (whitelistMode)
        {
            return catAdd;
        }

        if (catRemove) return false;
        if (catAdd) return true;
        return null;
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
