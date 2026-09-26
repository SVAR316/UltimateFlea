using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record LevelLocksConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>Start from SPT's live-like tieredFlea levels, then apply items/categories on top.</summary>
    [JsonPropertyName("useSptDefaults")]
    public bool UseSptDefaults { get; set; } = true;

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
