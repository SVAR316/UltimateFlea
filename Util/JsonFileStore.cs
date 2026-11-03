using System.Text.Json;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;

namespace UltimateFlea.Util;

/// <summary>
/// Single place for JSON file IO: serializer presets, tolerant reads and crash-safe writes.
/// </summary>
/// <remarks>
/// The logger is passed as <see cref="ISptLogger{T}"/>, which is generic over its owner, so every
/// method here carries a second type parameter for that owner. It is inferred everywhere except in
/// <see cref="ReadOrDefault{T, TOwner}"/>, where the payload type has to be named anyway.
/// </remarks>
public static class JsonFileStore
{
    /// <summary>Sub-folder of the mod folder that holds generated data files.</summary>
    public const string DataDirName = "data";

    /// <summary>Hand-edited config files: indented and tolerant about property name casing.</summary>
    public static readonly JsonSerializerOptions Config = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Generated data files: never indented, these hold up to ~20k entries.</summary>
    public static readonly JsonSerializerOptions Data = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Data folder for a mod folder, shared by every data store.</summary>
    public static string DataDir(string modPath) => Path.Combine(modPath, DataDirName);

    /// <summary>
    /// Reads <paramref name="path"/>, or returns default when the file does not exist.
    /// A file that exists but cannot be parsed is reported at Error level: silently falling back to
    /// defaults would quietly throw away user data.
    /// </summary>
    public static T? ReadOrDefault<T, TOwner>(
        string path,
        JsonSerializerOptions opts,
        ISptLogger<TOwner> logger,
        string what)
    {
        if (!File.Exists(path))
        {
            logger.Debug($"[UltimateFlea] No {what} at '{path}' yet, using defaults.");
            return default;
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                logger.Error($"[UltimateFlea] {what} at '{path}' is empty, using defaults. Fix or delete the file.");
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, opts);
        }
        catch (Exception ex)
        {
            logger.Error(
                $"[UltimateFlea] Failed to parse {what} at '{path}', using defaults. Fix or delete the file. ({ex.Message})",
                ex);
            return default;
        }
    }

    /// <summary>
    /// Serializes to "path.tmp" and moves it over the real file, so the primary file is never
    /// left truncated. The previous content is kept as "path.bak".
    /// </summary>
    public static bool WriteAtomic<T, TOwner>(
        string path,
        T value,
        JsonSerializerOptions opts,
        ISptLogger<TOwner> logger,
        string what,
        out string error)
    {
        error = string.Empty;
        var tmp = path + ".tmp";

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(tmp, JsonSerializer.Serialize(value, opts));

            // Keep the last good copy before the swap; the move itself is the only write to 'path'.
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            logger.Error($"[UltimateFlea] Failed to write {what} to '{path}': {ex.Message}", ex);
            TryDelete(tmp);
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup of the temp file, nothing to report.
        }
    }
}
