using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace UltimateFlea.Pool;

// WTT кладёт кастом в ItemConfig.Blacklist + кэш — без чистки офферов не будет.
[Injectable(InjectionType.Singleton)]
public class ItemBlacklistCleaner(
    ISptLogger<ItemBlacklistCleaner> logger,
    ConfigServer configServer,
    ItemFilterService itemFilterService)
{
    private static readonly FieldInfo? CacheField =
        typeof(ItemFilterService).GetField(
            "ItemBlacklistCache",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    public int Unblacklist(IEnumerable<MongoId> tpls)
    {
        var itemConfig = configServer.GetConfig<ItemConfig>();
        var cache = CacheField?.GetValue(itemFilterService) as HashSet<MongoId>;

        var cleared = 0;
        foreach (var tpl in tpls)
        {
            var fromConfig = itemConfig.Blacklist.Remove(tpl);
            var fromCache = cache?.Remove(tpl) ?? false;
            if (fromConfig || fromCache)
            {
                cleared++;
            }
        }

        if (cleared > 0)
        {
            logger.Info($"[UltimateFlea] Cleared {cleared} item(s) from global item blacklist/cache.");
        }

        return cleared;
    }
}
