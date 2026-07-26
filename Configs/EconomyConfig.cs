using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record EconomyConfig
{
    [JsonPropertyName("simIntervalMinutes")]
    public double SimIntervalMinutes { get; set; } = 30;

    [JsonPropertyName("demandPerBuy")]
    public double DemandPerBuy { get; set; } = 0.05;

    [JsonPropertyName("supplyPerSell")]
    public double SupplyPerSell { get; set; } = 0.05;

    [JsonPropertyName("priceElasticity")]
    public double PriceElasticity { get; set; } = 0.5;

    [JsonPropertyName("decayPerTick")]
    public double DecayPerTick { get; set; } = 0.1;

    [JsonPropertyName("settleSpeed")]
    public double SettleSpeed { get; set; } = 0.15;

    [JsonPropertyName("noise")]
    public double Noise { get; set; } = 0.03;

    [JsonPropertyName("minPriceFactor")]
    public double MinPriceFactor { get; set; } = 0.5;

    [JsonPropertyName("maxPriceFactor")]
    public double MaxPriceFactor { get; set; } = 4.0;

    [JsonPropertyName("wipe")]
    public WipeConfig Wipe { get; set; } = new();
}

/// <summary>Ранний вайп от RegistrationDate персонажа.</summary>
public record WipeConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("startLengthDays")]
    public double StartLengthDays { get; set; } = 10.0;

    /// <summary>Множитель в день 0, к концу фазы уходит в 1.0.</summary>
    [JsonPropertyName("startMultiplier")]
    public double StartMultiplier { get; set; } = 3.0;
}
