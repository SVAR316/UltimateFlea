using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record EventsConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("events")]
    public List<MarketEventEntry> Events { get; set; } = new();
}

/// <summary>Timed price bump from wipe day startDay..endDay.</summary>
public record MarketEventEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Wipe age in days when the event starts (inclusive).</summary>
    [JsonPropertyName("startDay")]
    public double StartDay { get; set; }

    /// <summary>Wipe age in days when the event ends (exclusive).</summary>
    [JsonPropertyName("endDay")]
    public double EndDay { get; set; }

    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; } = 1.0;

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}
