using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record ModConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("enablePoolManager")]
    public bool EnablePoolManager { get; set; } = true;

    [JsonPropertyName("enablePricing")]
    public bool EnablePricing { get; set; } = true;

    [JsonPropertyName("enableEconomy")]
    public bool EnableEconomy { get; set; } = true;

    /// <summary>local = handbook/prices.json. tarkovdev — на потом.</summary>
    [JsonPropertyName("priceSource")]
    public string PriceSource { get; set; } = "local";

    /// <summary>Иначе SPT после нас перезапишет prices из handbook.</summary>
    [JsonPropertyName("preserveBasePrices")]
    public bool PreserveBasePrices { get; set; } = true;

    /// <summary>Иначе цены ниже трейдера просто поднимаются.</summary>
    [JsonPropertyName("disableTraderPriceFloor")]
    public bool DisableTraderPriceFloor { get; set; } = true;

    /// <summary>Убирает рандом 0.8–1.2 на офферах. Влияет на весь flea.</summary>
    [JsonPropertyName("exactOfferPrices")]
    public bool ExactOfferPrices { get; set; } = false;

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;
}
