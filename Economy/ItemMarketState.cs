using System.Text.Json.Serialization;

namespace UltimateFlea.Economy;

public record ItemMarketState
{
    [JsonPropertyName("basePrice")]
    public double BasePrice { get; set; }

    [JsonPropertyName("currentPrice")]
    public double CurrentPrice { get; set; }

    // ~-1..1, ноль = нейтрально
    [JsonPropertyName("demand")]
    public double Demand { get; set; }

    [JsonPropertyName("supply")]
    public double Supply { get; set; }
}
