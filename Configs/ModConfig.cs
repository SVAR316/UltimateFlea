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

    /// <summary>
    /// Which LIVE provider to use when <see cref="LiveFlea"/> is on: tarkovdev (json.tarkov.dev
    /// avg24h) or tarkovmarket (needs <see cref="TarkovMarketApiKey"/>). "local" is the legacy
    /// way to say "no live source".
    /// </summary>
    [JsonPropertyName("priceSource")]
    public string PriceSource { get; set; } = "tarkovdev";

    /// <summary>
    /// true = live online prices from the provider in priceSource; false = local handbook prices
    /// only, the network is never touched. Null (key absent in an old mod.json) is resolved by
    /// <see cref="ResolveLiveFlea"/> and normalised to a concrete value on load.
    /// </summary>
    [JsonPropertyName("liveFlea")]
    public bool? LiveFlea { get; set; }

    /// <summary>Optional key for priceSource = tarkovmarket. Never logged.</summary>
    [JsonPropertyName("tarkovMarketApiKey")]
    public string TarkovMarketApiKey { get; set; } = "";

    /// <summary>Per-attempt timeout of the live price download (the dump is ~17 MB).</summary>
    [JsonPropertyName("priceFetchTimeoutSeconds")]
    public int PriceFetchTimeoutSeconds { get; set; } = 180;

    /// <summary>Back-compat: an old mod.json without liveFlea is live exactly when priceSource named a live provider.</summary>
    public bool ResolveLiveFlea() => LiveFlea ?? IsLiveProviderName(PriceSource);

    public static bool IsLiveProviderName(string? name) =>
        string.Equals(name, "tarkovdev", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "tarkovmarket", StringComparison.OrdinalIgnoreCase);

    /// <summary>Для tarkovdev: pve вместо regular.</summary>
    [JsonPropertyName("pvePrices")]
    public bool PvePrices { get; set; } = false;

    /// <summary>Иначе SPT после нас перезапишет prices из handbook.</summary>
    [JsonPropertyName("preserveBasePrices")]
    public bool PreserveBasePrices { get; set; } = true;

    /// <summary>Иначе цены ниже трейдера просто поднимаются.</summary>
    [JsonPropertyName("disableTraderPriceFloor")]
    public bool DisableTraderPriceFloor { get; set; } = false;

    /// <summary>
    /// How individual offers are spread around the reference price: "live" (per-item band from the
    /// 24h low/high of the live feed, items without it fall back to SPT's range), "spt" (SPT's
    /// global 0.8-1.2 range) or "exact" (every offer at the reference price). Null (key absent) is
    /// resolved from the deprecated <see cref="ExactOfferPrices"/> and normalised on load.
    /// </summary>
    [JsonPropertyName("offerSpread")]
    public string? OfferSpread { get; set; } = SpreadLive;

    /// <summary>Deprecated alias of offerSpread: true = "exact", false = "spt". Only read when offerSpread is absent.</summary>
    [JsonPropertyName("exactOfferPrices")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ExactOfferPrices { get; set; }

    public const string SpreadLive = "live";
    public const string SpreadSpt = "spt";
    public const string SpreadExact = "exact";

    /// <summary>Concrete offer spread mode; works on a config that has not been validated yet.</summary>
    public string ResolveOfferSpread()
    {
        var value = OfferSpread?.Trim().ToLowerInvariant();
        if (value is SpreadLive or SpreadSpt or SpreadExact)
        {
            return value;
        }

        return ExactOfferPrices switch
        {
            true => SpreadExact,
            false => SpreadSpt,
            _ => SpreadLive
        };
    }

    /// <summary>Только лоты игроков: без виртуальных (FakePlayer) и без ассорта торговцев на flea.</summary>
    [JsonPropertyName("playerOffersOnly")]
    public bool PlayerOffersOnly { get; set; } = false;

    /// <summary>ПКМ в схроне: мгновенная выставка на барахолку.</summary>
    [JsonPropertyName("quickListFromStash")]
    public bool QuickListFromStash { get; set; } = true;

    [JsonPropertyName("debug")]
    public bool Debug { get; set; } = false;
}
