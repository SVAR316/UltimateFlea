using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record EventsConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("events")]
    public List<MarketEventEntry> Events { get; set; } = DefaultEvents();

    /// <summary>Mirrors config/events.json so an install without that file behaves like a fresh one.</summary>
    private static List<MarketEventEntry> DefaultEvents()
    {
        string[] barter =
        [
            "57864a3d24597754843f8721", "57864a66245977548f04a81f",
            "57864bb7245977548b3b66c2", "57864c322459775490116fbf"
        ];
        return
        [
            new() { Id = "early_hideout_rush", StartDay = 0, EndDay = 10, Multiplier = 1.15, Categories = [.. barter] },
            new() { Id = "mid_ammo_pressure", StartDay = 8, EndDay = 24, Multiplier = 1.12,
                    Categories = ["5485a8684bdc2da71d8b4567", "543be5cb4bdc2deb348b4568"] },
            new() { Id = "late_barter_soft", StartDay = 28, EndDay = 90, Multiplier = 0.9, Categories = [.. barter] },
        ];
    }
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
