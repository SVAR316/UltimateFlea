using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding;

namespace UltimateFlea.Web;

/// <summary>
/// Where every item came from. Immutable once built.
/// </summary>
/// <param name="TplToGuid">Items attributed to one mod GUID.</param>
/// <param name="GuidToSource">The mods that own at least one item.</param>
/// <param name="Unknown">Items that are not vanilla but could not be tied to exactly one mod.</param>
/// <param name="Vanilla">Ids of the vanilla item list on disk, or null when it could not be read.</param>
/// <param name="NonVanilla">Every item type "Item" that is not in the vanilla list (null baseline: empty).</param>
public sealed record ItemSourceMap(
    IReadOnlyDictionary<MongoId, string> TplToGuid,
    IReadOnlyDictionary<string, CatalogSource> GuidToSource,
    IReadOnlySet<MongoId> Unknown,
    IReadOnlySet<string>? Vanilla,
    IReadOnlyList<MongoId> NonVanilla);

/// <summary>
/// Works out which mod added which item, once, on first use (after all mods loaded).
/// </summary>
/// <remarks>
/// Order of evidence: SPT's mod item cache first, then "not in the vanilla list on disk" plus the
/// mods' own JSON files. In the live install the cache reported nothing although mods had added 837
/// items, so it cannot be the only source. Only an id found in the vanilla list is ever vanilla.
/// </remarks>
[Injectable(InjectionType.Singleton)]
public class ItemSourceResolver(
    ISptLogger<ItemSourceResolver> logger,
    ConfigManager configManager,
    TemplateTable templates,
    ModItemCacheService modItemCache,
    IReadOnlyList<SptMod> loadedMods)
{
    /// <summary>Wire value of an item's <c>src</c> when its origin cannot be established.</summary>
    public const int UnknownSource = -1;

    private readonly object _gate = new();
    private volatile ItemSourceMap? _map;

    public ItemSourceMap Get()
    {
        var existing = _map;
        if (existing is not null)
        {
            return existing;
        }

        lock (_gate)
        {
            return _map ??= Build();
        }
    }

    private ItemSourceMap Build()
    {
        var clock = Stopwatch.StartNew();
        var items = templates.Items;

        var (tplToGuid, guidToSource) = BuildFromCache();
        var vanilla = LoadVanillaBaseline();
        var (unknown, nonVanilla) = ResolveUncredited(items, vanilla, tplToGuid, guidToSource);

        logger.Info($"[UltimateFlea] Item sources resolved in {clock.ElapsedMilliseconds} ms.");
        return new ItemSourceMap(tplToGuid, guidToSource, unknown, vanilla, nonVanilla);
    }

    // tpl -> GUID, inverted once. This mod's own GUID and SPT's are skipped so core items are not shown as a mod.
    private (Dictionary<MongoId, string>, Dictionary<string, CatalogSource>) BuildFromCache()
    {
        var ownGuid = new ModMetadata().ModGuid;
        var tplToGuid = new Dictionary<MongoId, string>();
        var guidToSource = new Dictionary<string, CatalogSource>(StringComparer.OrdinalIgnoreCase);

        var meta = MetaByGuid();

        var nextId = 1;
        foreach (var (guid, ids) in modItemCache.GetAllCachedModItemIds().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(guid, ownGuid, StringComparison.OrdinalIgnoreCase)
                || guid.StartsWith("com.spt", StringComparison.OrdinalIgnoreCase)
                || guid.StartsWith("spt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var added = false;
            foreach (var id in ids)
            {
                // First mod wins if two claim the same id.
                if (tplToGuid.TryAdd(id, guid))
                {
                    added = true;
                }
            }

            if (!added)
            {
                continue;
            }

            meta.TryGetValue(guid, out var m);
            var name = string.IsNullOrWhiteSpace(m?.Name) ? guid : m!.Name;
            guidToSource[guid] = new CatalogSource(nextId++, guid, name, m?.Author ?? string.Empty);
        }

        return (tplToGuid, guidToSource);
    }

    private Dictionary<string, IModMetadata> MetaByGuid() =>
        loadedMods
            .Where(m => m.ModMetadata is not null)
            .GroupBy(m => m.ModMetadata.ModGuid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ModMetadata, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The ids of the vanilla item database as shipped on disk (SPT_Data/database/templates/items.json).
    /// Mods add their items to the in-memory database at startup and never edit that file, so an id
    /// that is in the live database but not in this file was added by something. Null when the file
    /// cannot be found; then no item may be called vanilla.
    /// </summary>
    private HashSet<string>? LoadVanillaBaseline()
    {
        try
        {
            // <runtime>/user/mods/<ThisMod> -> <runtime>/SPT_Data/database/templates/items.json
            var user = Directory.GetParent(configManager.ModPath.TrimEnd('\\', '/'))?.Parent;
            var runtime = user?.Parent;
            if (runtime is null)
            {
                return null;
            }

            var path = System.IO.Path.Combine(runtime.FullName, "SPT_Data", "database", "templates", "items.json");
            if (!File.Exists(path))
            {
                logger.Info("[UltimateFlea] Vanilla item list not found at the expected place, item sources will be shown as unknown.");
                return null;
            }

            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                ids.Add(prop.Name);
            }

            return ids;
        }
        catch (Exception ex)
        {
            logger.Info($"[UltimateFlea] Could not read the vanilla item list, item sources will be shown as unknown: {ex.Message}");
            return null;
        }
    }

    // A bare 24-digit hex token, the form of a template id inside a mod's JSON.
    private static readonly Regex IdToken = new("(?<![0-9a-fA-F])[0-9a-fA-F]{24}(?![0-9a-fA-F])", RegexOptions.Compiled);

    /// <summary>
    /// Items the mod item cache did not credit. Those that are not in the vanilla file were added by a
    /// mod that bypassed the cache. They are attributed by reading each mod's own JSON files: an item
    /// goes to the single mod whose files mention its id. Zero or several mentioning mods leave it unknown.
    /// </summary>
    /// <remarks>
    /// What this can get wrong: a mod that merely references another mod's item id (a config naming
    /// it) counts as a claimant, and an item whose id is computed in code or stored in a binary file
    /// is never found. Ambiguity is resolved to unknown, never guessed.
    /// </remarks>
    private (HashSet<MongoId> Unknown, List<MongoId> NonVanilla) ResolveUncredited(
        IReadOnlyDictionary<MongoId, TemplateItem> items,
        HashSet<string>? vanilla,
        Dictionary<MongoId, string> tplToGuid,
        Dictionary<string, CatalogSource> guidToSource)
    {
        var unknown = new HashSet<MongoId>();
        if (vanilla is null)
        {
            return (unknown, []);
        }

        var nonVanilla = items
            .Where(kv => kv.Value.Type == "Item" && !vanilla.Contains(kv.Key.ToString()))
            .Select(kv => kv.Key)
            .ToList();

        var added = nonVanilla.Where(id => !tplToGuid.ContainsKey(id)).ToList();
        if (added.Count == 0)
        {
            return (unknown, nonVanilla);
        }

        var addedSet = added.ToDictionary(id => id.ToString(), id => id, StringComparer.OrdinalIgnoreCase);
        var ownGuid = new ModMetadata().ModGuid;
        var claims = new Dictionary<MongoId, HashSet<string>>();
        var definitions = new Dictionary<MongoId, HashSet<string>>();
        var filesRead = 0;
        long bytesRead = 0;
        const long ScanBudgetBytes = 256L * 1024 * 1024;

        foreach (var mod in loadedMods)
        {
            var meta = mod.ModMetadata;
            if (meta is null || string.IsNullOrEmpty(mod.Directory)
                || string.Equals(meta.ModGuid, ownGuid, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(mod.Directory, "*.json*", SearchOption.AllDirectories))
                {
                    // Item definition files are small; skip huge files and stop at a total budget so the first request stays quick.
                    var length = new FileInfo(file).Length;
                    if (length > 32 * 1024 * 1024 || bytesRead + length > ScanBudgetBytes)
                    {
                        continue;
                    }

                    filesRead++;
                    bytesRead += length;
                    var text = File.ReadAllText(file);
                    var mentioned = new HashSet<MongoId>();
                    foreach (Match m in IdToken.Matches(text))
                    {
                        if (addedSet.TryGetValue(m.Value, out var id))
                        {
                            mentioned.Add(id);
                        }
                    }

                    if (mentioned.Count == 0)
                    {
                        continue;
                    }

                    foreach (var id in mentioned)
                    {
                        Claim(claims, id, meta.ModGuid);
                    }

                    foreach (var id in FindDefinitions(text, addedSet))
                    {
                        Claim(definitions, id, meta.ModGuid);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Info($"[UltimateFlea] Could not scan mod folder '{mod.Directory}' for item definitions: {ex.Message}");
            }
        }

        var metaByGuid = MetaByGuid();
        var nextId = guidToSource.Values.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1;
        var attributed = 0;

        foreach (var id in added)
        {
            // A definition beats a mere mention: another mod listing the id in a config, quest or trader file
            // is a reference, not the author. Two definers, or no definer and several mentioners, stay unknown.
            var guids = definitions.TryGetValue(id, out var defined) ? defined : claims.GetValueOrDefault(id);
            if (guids is { Count: 1 })
            {
                var guid = guids.First();
                if (!guidToSource.ContainsKey(guid))
                {
                    metaByGuid.TryGetValue(guid, out var m);
                    var name = string.IsNullOrWhiteSpace(m?.Name) ? guid : m!.Name;
                    guidToSource[guid] = new CatalogSource(nextId++, guid, name, m?.Author ?? string.Empty);
                }

                tplToGuid[id] = guid;
                attributed++;
            }
            else
            {
                unknown.Add(id);
            }
        }

        logger.Info(
            $"[UltimateFlea] {added.Count} item(s) are not in the vanilla list and were not claimed by SPT's mod item cache: " +
            $"{attributed} attributed by reading {filesRead} mod JSON file(s), {unknown.Count} left as unknown source.");

        return (unknown, nonVanilla);
    }

    private static void Claim(Dictionary<MongoId, HashSet<string>> map, MongoId id, string guid)
    {
        if (!map.TryGetValue(id, out var set))
        {
            map[id] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        set.Add(guid);
    }

    private static readonly JsonDocumentOptions LenientJson = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    // Properties that only an item definition has: the clone-an-item format used by WTT CommonLib mods,
    // and the raw database format.
    private static readonly string[] DefinitionMarkers = ["itemTplToClone", "_props", "_parent", "overrideProperties"];

    /// <summary>
    /// Ids that this file DEFINES, as opposed to merely mentions: the id is a top-level key whose value
    /// is an object carrying a definition marker. An id used as a value in a trader assort, a quest, a
    /// locale or a list of masks is a reference and is not returned. A file in another layout (or not
    /// parseable) returns nothing, which falls back to the mention rule.
    /// </summary>
    private static IEnumerable<MongoId> FindDefinitions(string text, Dictionary<string, MongoId> addedSet)
    {
        var found = new List<MongoId>();
        try
        {
            using var doc = JsonDocument.Parse(text, LenientJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return found;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object
                    && addedSet.TryGetValue(prop.Name, out var id)
                    && DefinitionMarkers.Any(marker => prop.Value.TryGetProperty(marker, out _)))
                {
                    found.Add(id);
                }
            }
        }
        catch (JsonException)
        {
            // Not valid JSON for the lenient parser: treat as mentions only.
        }

        return found;
    }
}
