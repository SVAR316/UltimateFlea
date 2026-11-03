using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using UltimateFlea.Web;

namespace UltimateFlea.Presets;

/// <summary>Items from one source mod that the preset would add.</summary>
public sealed record ModItemCount(string ModName, int Count);

/// <summary>
/// Result of a probe: the TPLs to add and where they came from, or why there are none.
/// <see cref="UnknownSourceCount"/> items are included in <see cref="Tpls"/> but not in <see cref="ByMod"/>.
/// </summary>
public sealed record ModdedItemsProbe(
    IReadOnlyList<string> Tpls,
    IReadOnlyList<ModItemCount> ByMod,
    int UnknownSourceCount,
    LocalizedText? UnavailableReason);

/// <summary>
/// Finds items that third-party mods added to the item database, without any hardcoded item id.
/// </summary>
/// <remarks>
/// A candidate is an item that is not in the vanilla item list on disk (see <see cref="ItemSourceResolver"/>),
/// plus anything SPT's mod item cache credits to a mod. The cache alone is not enough: on a real install
/// it reported nothing while mods had added hundreds of items.
///
/// Items whose source mod cannot be pinned down are still included. They are demonstrably not vanilla,
/// i.e. added by some installed mod, which is exactly what this preset promises to put on the flea;
/// leaving them out would silently drop real mod items. The apply warning reports how many there were.
///
/// An item is skipped when it should not be a flea good even though a mod added it:
/// - <c>Properties.QuestItem</c> (quest items),
/// - <c>Properties.IsUnsaleable</c> (the mod explicitly forbids selling it),
/// - a base class of loot container, stationary container, stash, sorting table, pockets or inventory
///   (world containers and corpses are children of these),
/// - no price at all: neither a flea price nor a positive handbook price. The economy engine ignores
///   priceless items anyway, so there is no fallback price; the item is simply left out.
/// </remarks>
[Injectable(InjectionType.Singleton)]
public class ModdedItemsDetector(TemplateTable templates, ItemSourceResolver sourceResolver)
{
    // Item-tree base classes (items.json) whose descendants are containers, not tradable goods.
    private static readonly HashSet<MongoId> NonGoodsBaseClasses =
    [
        new("566965d44bdc2d814c8b4571"), // LootContainer
        new("567583764bdc2d98058b456e"), // StationaryContainer
        new("566abbb64bdc2d144c8b457d"), // Stash
        new("6050cac987d3f925bf016837"), // SortingTable
        new("62f109593b54472778797866"), // RandomLootContainer
        new("557596e64bdc2dc2118b4571"), // Pockets
        new("55d720f24bdc2d88028b456d"), // Inventory
    ];

    public ModdedItemsProbe Probe()
    {
        var items = templates.Items;
        var prices = templates.Prices;
        var map = sourceResolver.Get();

        var handbook = templates.Handbook.Items
            .Where(h => h.Price is > 0)
            .Select(h => h.Id)
            .ToHashSet();

        var candidates = new HashSet<MongoId>(map.NonVanilla);
        candidates.UnionWith(map.TplToGuid.Keys);

        var tpls = new List<string>();
        var perMod = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unknown = 0;

        foreach (var id in candidates)
        {
            if (!items.TryGetValue(id, out var item) || !IsFleaGood(item, items)
                || !((prices.TryGetValue(id, out var p) && p > 0) || handbook.Contains(id)))
            {
                continue;
            }

            tpls.Add(id.ToString());

            if (map.TplToGuid.TryGetValue(id, out var guid) && map.GuidToSource.TryGetValue(guid, out var source))
            {
                perMod[source.Name] = perMod.GetValueOrDefault(source.Name) + 1;
            }
            else
            {
                unknown++;
            }
        }

        if (tpls.Count == 0)
        {
            return new([], [], 0, new(
                "No tradable items added by other mods were found. Either no item-adding mods are installed, or their items are quest items, containers, unsaleable or have no price.",
                "Не найдено торгуемых предметов, добавленных другими модами. Либо моды с новыми предметами не установлены, либо их предметы квестовые, контейнеры, непродаваемые или без цены."));
        }

        tpls.Sort(StringComparer.Ordinal);
        var byMod = perMod
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ModItemCount(kv.Key, kv.Value))
            .ToList();

        return new(tpls, byMod, unknown, null);
    }

    private static bool IsFleaGood(TemplateItem item, IReadOnlyDictionary<MongoId, TemplateItem> all)
    {
        if (item.Type != "Item" || item.Properties is null)
        {
            return false;
        }

        if (item.Properties.QuestItem == true || item.Properties.IsUnsaleable == true)
        {
            return false;
        }

        // Walk up the parent chain; the depth cap guards against a cyclic parent in a broken mod.
        var parent = item.Parent;
        for (var depth = 0; depth < 32; depth++)
        {
            if (NonGoodsBaseClasses.Contains(parent))
            {
                return false;
            }

            if (!all.TryGetValue(parent, out var node))
            {
                break;
            }

            parent = node.Parent;
        }

        return true;
    }
}
