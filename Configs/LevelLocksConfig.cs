using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record LevelLocksConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Base table the user's items/categories are merged on top of: "bundled" (default, config/levels_bundled.json),
    /// "spt" (ragfair tieredFlea) or "none". Null in a file means "absent": resolved in ConfigManager.ValidateLevels.
    /// </summary>
    [JsonPropertyName("baseTable")]
    public string? BaseTable { get; set; } = "bundled";

    /// <summary>Deprecated alias of baseTable (true = "spt", false = "none"); only read when baseTable is absent.</summary>
    [JsonPropertyName("useSptDefaults")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UseSptDefaults { get; set; }

    [JsonPropertyName("lockBuying")]
    public bool LockBuying { get; set; } = true;

    [JsonPropertyName("lockSelling")]
    public bool LockSelling { get; set; } = true;

    /// <summary>Item TPL -> required level. 0 removes a default lock.</summary>
    [JsonPropertyName("items")]
    public Dictionary<string, int> Items { get; set; } = new();

    /// <summary>Base class / category ID -> required level (nested children included). 0 removes a default lock.</summary>
    [JsonPropertyName("categories")]
    public Dictionary<string, int> Categories { get; set; } = new();
}
