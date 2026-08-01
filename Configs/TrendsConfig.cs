using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record TrendsConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("trends")]
    public List<CategoryTrendEntry> Trends { get; set; } = new();
}

/// <summary>Slow sine wave on a parent category over wipe age.</summary>
public record CategoryTrendEntry
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("periodDays")]
    public double PeriodDays { get; set; } = 21.0;

    /// <summary>Peak deviation from 1.0. 0.15 means about +/-15%.</summary>
    [JsonPropertyName("amplitude")]
    public double Amplitude { get; set; } = 0.15;

    [JsonPropertyName("phase")]
    public double Phase { get; set; }
}
