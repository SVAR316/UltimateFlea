using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace UltimateFlea.Pool;

// Пул на flea: CanSellOnRagfair + custom blacklist + ItemConfig.Blacklist (нужно для WTT).
[Injectable(InjectionType.Singleton)]
public class PoolManager(
    ISptLogger<PoolManager> logger,
    ConfigManager configManager,
    DatabaseServer databaseServer,
    ConfigServer configServer,
    ItemBlacklistCleaner itemBlacklistCleaner)
{
    public void Apply()
    {
        var poolConfig = configManager.Pool;
        var items = databaseServer.GetTables().Templates.Items;
        var ragfairConfig = configServer.GetConfig<RagfairConfig>();
        var itemConfig = configServer.GetConfig<ItemConfig>();

        var addTpls = ToMongoIdSet(poolConfig.Add);
        var removeTpls = ToMongoIdSet(poolConfig.Remove);
        var addCats = ToMongoIdSet(poolConfig.AddCategories);
        var removeCats = ToMongoIdSet(poolConfig.RemoveCategories);

        var whitelistMode = string.Equals(poolConfig.Mode, "whitelist", StringComparison.OrdinalIgnoreCase);

        var enabled = 0;
        var disabled = 0;
        var toUnblacklist = new List<MongoId>();

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
                ragfairConfig.Dynamic.Blacklist.Custom.Remove(tpl);
                toUnblacklist.Add(tpl);
                enabled++;
            }
            else
            {
                item.Properties.CanSellOnRagfair = false;
                ragfairConfig.Dynamic.Blacklist.Custom.Add(tpl);
                itemConfig.Blacklist.Add(tpl);
                disabled++;
            }
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
            return catAdd ? true : false;
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
                logger.Warning($"[UltimateFlea] Ignoring invalid TPL in pool config: '{s}' (get IDs from db.sp-tarkov.com)");
                continue;
            }

            set.Add(new MongoId(s));
        }

        return set;
    }
}
