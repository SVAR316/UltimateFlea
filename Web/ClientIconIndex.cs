using System.Text.Json;
using System.Text.RegularExpressions;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Web;

/// <summary>
/// Maps template ids to PNG files in the game client's own icon cache on the user's disk.
/// The files are BSG assets: they are only ever read from where the client wrote them and served
/// to the local dashboard, never copied into the mod or a build output.
/// </summary>
public static partial class ClientIconIndex
{
    // Only "<digits>.png" is ever accepted as a cache file name.
    [GeneratedRegex(@"^[0-9]{1,9}\.png$")]
    private static partial Regex CacheFileName();

    /// <summary>
    /// The cache lives at &lt;server&gt;/user/sptappdata/&lt;version&gt;/, outside the mod folder. The server's
    /// user folder is found structurally from the mod folder (&lt;user&gt;/mods/&lt;ThisMod&gt;); when the layout
    /// is not exactly that, the result is empty rather than a guessed path.
    /// </summary>
    public static IReadOnlyList<string> FindCacheDirs(string modPath)
    {
        if (string.IsNullOrEmpty(modPath))
        {
            return [];
        }

        var mods = Directory.GetParent(modPath.TrimEnd('\\', '/'));
        var user = mods?.Parent;
        if (mods is null || user is null
            || !string.Equals(mods.Name, "mods", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.Name, "user", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var appData = Path.Combine(user.FullName, "sptappdata");
        if (!Directory.Exists(appData))
        {
            return [];
        }

        try
        {
            return Directory.GetDirectories(appData)
                .Where(d => File.Exists(Path.Combine(d, "index.json")))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Returns tpl -> absolute PNG path. Never throws: a missing or corrupt cache gives an empty map.
    /// A key that more than one template could have produced is ambiguous and is dropped.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Build<TOwner>(
        string modPath,
        IEnumerable<string> tpls,
        ISptLogger<TOwner> logger)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dirs = FindCacheDirs(modPath);

        if (dirs.Count == 0)
        {
            logger.Info("[UltimateFlea] No game icon cache found next to this server, items will have no pictures.");
            return result;
        }

        var tplList = tpls.ToList();

        foreach (var dir in dirs)
        {
            Dictionary<int, int> index;
            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(Path.Combine(dir, "index.json")));
                index = new Dictionary<int, int>();
                foreach (var (k, v) in raw ?? [])
                {
                    if (int.TryParse(k, out var key))
                    {
                        index[key] = v;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Info($"[UltimateFlea] Icon cache index in '{dir}' could not be read, skipping it: {ex.Message}");
                continue;
            }

            // key -> templates that can produce it, so ambiguous keys can be rejected.
            var owners = new Dictionary<int, string?>();
            foreach (var tpl in tplList)
            {
                foreach (var key in ClientIconKey.Candidates(tpl))
                {
                    if (!index.ContainsKey(key))
                    {
                        continue;
                    }

                    owners[key] = owners.TryGetValue(key, out var existing) && existing != tpl ? null : tpl;
                }
            }

            var added = 0;
            foreach (var (key, tpl) in owners)
            {
                if (tpl is null || result.ContainsKey(tpl))
                {
                    continue;
                }

                var name = $"{index[key]}.png";
                if (!CacheFileName().IsMatch(name))
                {
                    continue;
                }

                var path = Path.Combine(dir, name);
                if (File.Exists(path))
                {
                    result[tpl] = path;
                    added++;
                }
            }

            logger.Info($"[UltimateFlea] Icon cache '{Path.GetFileName(dir)}': {index.Count} entries, {added} matched to items.");
        }

        return result;
    }
}
