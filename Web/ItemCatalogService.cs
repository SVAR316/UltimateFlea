using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;

using SPTarkov.Server.Core.Services.Image;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Locales;

namespace UltimateFlea.Web;

public sealed record CatalogSource(int Id, string Guid, string Name, string Author);

public sealed record CatalogCategory(string Id, string Name, string NameRu, string? Icon);

/// <summary>Short property names on purpose: the names repeat on every one of thousands of rows.</summary>
public sealed record CatalogItem(
    string Id,
    string En,
    string Ru,
    string Se,
    string Sr,
    string Cat,
    int Src,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? Ic = null);

public sealed record CatalogPayload(
    string Version,
    IReadOnlyList<CatalogSource> Sources,
    IReadOnlyList<CatalogCategory> Categories,
    IReadOnlyList<CatalogItem> Items);

/// <summary>Serialised catalogue, ready to send. Immutable; one instance per process.</summary>
public sealed record CatalogBlob(byte[] Json, byte[] Gzip, string ETag);

/// <summary>
/// Builds the item picker catalogue once, on first request, and keeps it for the process lifetime.
/// </summary>
/// <remarks>
/// Lazy on purpose: the first Russian locale load is about 4 MB and must not slow server startup.
/// Pictures: SPT ships no per-item icons, so only category glyphs from the handbook are offered, and
/// a glyph URL is emitted only when SPT's image router confirms it serves that file.
///
/// Source labels: the mod item cache credits an item first. In the live install it reported nothing
/// at all although mods had added 837 items (WTT writes into the item database without it being
/// recorded), so uncredited items are compared with the vanilla list on disk and, when not vanilla,
/// attributed from the mods' own JSON files (see ItemCatalogSources.cs). Only an id in the vanilla
/// list is ever called vanilla; everything else unresolved is "unknown".
/// </remarks>
[Injectable(InjectionType.Singleton)]
public class ItemCatalogService(
    ISptLogger<ItemCatalogService> logger,
    ConfigManager configManager,
    TemplateTable templates,
    LocaleService locales,
    ItemSourceResolver sourceResolver,
    ImageRouterService imageRouter)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly object _buildGate = new();
    private volatile CatalogBlob? _blob;

    // tpl -> PNG in the client icon cache. Replaced as a whole inside Build, read-only afterwards.
    private volatile IReadOnlyDictionary<string, string> _icons = new Dictionary<string, string>();

    public CatalogBlob Get()
    {
        var existing = _blob;
        if (existing is not null)
        {
            return existing;
        }

        lock (_buildGate)
        {
            // A failed build leaves _blob null, so the next request tries again.
            return _blob ??= Build();
        }
    }

    /// <summary>
    /// The cached PNG for a template id, only ever from the precomputed map. The request value is a
    /// dictionary key and is never used to build a path.
    /// </summary>
    public bool TryGetIcon(string tpl, out string path)
    {
        Get();
        return _icons.TryGetValue(tpl, out path!);
    }

    private CatalogBlob Build()
    {
        var items = templates.Items;
        var en = locales.GetLocaleDb("en");
        var ru = locales.GetLocaleDb("ru");

        var sources = sourceResolver.Get();
        var tplToGuid = sources.TplToGuid;
        var guidToSource = sources.GuidToSource;
        var vanilla = sources.Vanilla;
        var unknownSource = sources.Unknown;

        var icons = ClientIconIndex.Build(
            configManager.ModPath,
            items.Where(kv => kv.Value.Type == "Item").Select(kv => kv.Key.ToString()),
            logger);

        // Handbook category per item parent: the most common handbook parent among its items.
        var handbookParent = new Dictionary<MongoId, Dictionary<MongoId, int>>();
        foreach (var entry in templates.Handbook.Items)
        {
            var hp = entry.ParentId;
            if (!items.TryGetValue(entry.Id, out var t))
            {
                continue;
            }

            if (!handbookParent.TryGetValue(t.Parent, out var votes))
            {
                handbookParent[t.Parent] = votes = new();
            }

            votes[hp] = votes.GetValueOrDefault(hp) + 1;
        }

        var handbookIcons = templates.Handbook.Categories
            .Where(c => !string.IsNullOrEmpty(c.Icon))
            .ToDictionary(c => c.Id, c => c.Icon!);

        var rows = new List<CatalogItem>(items.Count);
        var usedCategories = new HashSet<MongoId>();
        var missingEn = 0;
        var missingRu = 0;

        foreach (var (tpl, item) in items.OrderBy(kv => kv.Key.ToString(), StringComparer.Ordinal))
        {
            if (item.Type != "Item")
            {
                continue;
            }

            var id = tpl.ToString();

            // Missing locale entries degrade instead of failing: ru -> en -> template name -> id.
            var nameEn = Pick(en, $"{id} Name") ?? NonBlank(item.Name) ?? id;
            if (Pick(en, $"{id} Name") is null) missingEn++;

            var nameRu = Pick(ru, $"{id} Name");
            if (nameRu is null) missingRu++;
            nameRu ??= nameEn;

            var shortEn = Pick(en, $"{id} ShortName") ?? nameEn;
            var shortRu = Pick(ru, $"{id} ShortName") ?? shortEn;

            // 0 = confirmed vanilla (present in the on-disk vanilla list), > 0 = a mod source,
            // -1 = origin unknown. "No mod claimed it" is never reported as vanilla.
            var src = tplToGuid.TryGetValue(tpl, out var guid) && guidToSource.TryGetValue(guid, out var s) ? s.Id
                : unknownSource.Contains(tpl) ? ItemSourceResolver.UnknownSource
                : vanilla is not null && vanilla.Contains(id) ? 0
                : ItemSourceResolver.UnknownSource;

            rows.Add(new CatalogItem(id, nameEn, nameRu, shortEn, shortRu, item.Parent.ToString(), src,
                icons.ContainsKey(id) ? true : null));
            usedCategories.Add(item.Parent);
        }

        var categories = new List<CatalogCategory>(usedCategories.Count);
        var iconChecked = 0;
        var iconDropped = 0;

        foreach (var cat in usedCategories.OrderBy(c => c.ToString(), StringComparer.Ordinal))
        {
            var catId = cat.ToString();
            items.TryGetValue(cat, out var node);

            var nameEn = Pick(en, $"{catId} Name") ?? NonBlank(node?.Name) ?? catId;
            var nameRu = Pick(ru, $"{catId} Name") ?? nameEn;

            string? icon = null;
            if (handbookParent.TryGetValue(cat, out var votes))
            {
                var best = votes.OrderByDescending(v => v.Value).First().Key;
                if (handbookIcons.TryGetValue(best, out var path))
                {
                    iconChecked++;
                    icon = RouterServes(path) ? path : null;
                    if (icon is null) iconDropped++;
                }
            }

            categories.Add(new CatalogCategory(catId, nameEn, nameRu, icon));
        }

        var payload = new CatalogPayload(
            ModMetadata.AssemblyVersion,
            guidToSource.Values.OrderBy(s => s.Id).ToList(),
            categories,
            rows);

        _icons = icons;
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var gzip = Compress(json);
        var etag = "\"" + Convert.ToHexString(SHA256.HashData(json))[..24].ToLowerInvariant() + "\"";

        logger.Info(
            $"[UltimateFlea] Item catalogue built: {rows.Count} items, {categories.Count} categories, " +
            $"{guidToSource.Count} mod source(s), {rows.Count(r => r.Src == ItemSourceResolver.UnknownSource)} unknown-source item(s), {rows.Count(r => r.Src == 0)} confirmed vanilla, {json.Length / 1024} KB ({gzip.Length / 1024} KB gzip). " +
            $"Missing names: en {missingEn}, ru {missingRu}. Category icons kept {iconChecked - iconDropped}/{iconChecked}. Item icons from the game cache: {icons.Count}/{rows.Count}.");

        return new CatalogBlob(json, gzip, etag);
    }

    // The router keys its routes by path; check both the full path and the path without extension,
    // and emit the URL only when one of them is really registered.
    private bool RouterServes(string path)
    {
        try
        {
            if (imageRouter.ExistsByKey(path))
            {
                return true;
            }

            var dot = path.LastIndexOf('.');
            return dot > 0 && imageRouter.ExistsByKey(path[..dot]);
        }
        catch
        {
            return false;
        }
    }

    private static string? Pick(Dictionary<string, string> db, string key) =>
        db.TryGetValue(key, out var v) ? NonBlank(v) : null;

    private static string? NonBlank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v;

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gz = new GZipStream(output, CompressionLevel.Optimal))
        {
            gz.Write(data);
        }

        return output.ToArray();
    }
}
