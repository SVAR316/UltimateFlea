using System.Text.Json.Serialization;

namespace UltimateFlea.LevelLocks;

// Payload for the client plugin (client/UltimateFlea.Client reads the same field names).
public record LevelLocksRules
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("lockBuying")]
    public bool LockBuying { get; set; }

    [JsonPropertyName("lockSelling")]
    public bool LockSelling { get; set; }

    /// <summary>Item TPL -> required player level, already resolved through categories.</summary>
    [JsonPropertyName("levels")]
    public Dictionary<string, int> Levels { get; set; } = new();

    /// <summary>Handbook category ID -> level shown next to the category in the flea tree.</summary>
    [JsonPropertyName("categories")]
    public Dictionary<string, int> Categories { get; set; } = new();
}
