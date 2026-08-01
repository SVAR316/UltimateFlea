using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record EconomyConfig
{
    [JsonPropertyName("simIntervalMinutes")]
    public double SimIntervalMinutes { get; set; } = 60;

    [JsonPropertyName("demandPerBuy")]
    public double DemandPerBuy { get; set; } = 0.03;

    [JsonPropertyName("supplyPerSell")]
    public double SupplyPerSell { get; set; } = 0.03;

    [JsonPropertyName("priceElasticity")]
    public double PriceElasticity { get; set; } = 0.3;

    [JsonPropertyName("decayPerTick")]
    public double DecayPerTick { get; set; } = 0.08;

    [JsonPropertyName("settleSpeed")]
    public double SettleSpeed { get; set; } = 0.1;

    [JsonPropertyName("noise")]
    public double Noise { get; set; } = 0.03;

    [JsonPropertyName("minPriceFactor")]
    public double MinPriceFactor { get; set; } = 0.7;

    [JsonPropertyName("maxPriceFactor")]
    public double MaxPriceFactor { get; set; } = 2.0;

    [JsonPropertyName("wipe")]
    public WipeConfig Wipe { get; set; } = new();
}

/// <summary>
/// Early-wipe bump from RegistrationDate.
/// Useful with local prices. With tarkovdev leave disabled, live averages already include wipe stage.
/// </summary>
public record WipeConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("startLengthDays")]
    public double StartLengthDays { get; set; } = 14.0;

    /// <summary>Day-zero multiplier, falls to 1.0 by end of phase.</summary>
    [JsonPropertyName("startMultiplier")]
    public double StartMultiplier { get; set; } = 1.8;
}
