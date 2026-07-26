using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record PricingConfig
{
    [JsonPropertyName("globalMultiplier")]
    public double GlobalMultiplier { get; set; } = 1.0;

    /// <summary>Жёсткая цена, симуляция не трогает.</summary>
    [JsonPropertyName("fixedPrices")]
    public Dictionary<string, double> FixedPrices { get; set; } = new();

    [JsonPropertyName("itemMultipliers")]
    public Dictionary<string, double> ItemMultipliers { get; set; } = new();

    [JsonPropertyName("categoryMultipliers")]
    public Dictionary<string, double> CategoryMultipliers { get; set; } = new();
}
