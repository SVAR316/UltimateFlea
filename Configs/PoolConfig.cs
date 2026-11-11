using System.Text.Json.Serialization;

namespace UltimateFlea.Configs;

public record PoolConfig
{
    /// <summary>blacklist = режем лишнее, whitelist = только то что в add.</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "blacklist";

    [JsonPropertyName("add")]
    public List<string> Add { get; set; } = new();

    [JsonPropertyName("remove")]
    public List<string> Remove { get; set; } = new();

    [JsonPropertyName("addCategories")]
    public List<string> AddCategories { get; set; } = new();

    [JsonPropertyName("removeCategories")]
    public List<string> RemoveCategories { get; set; } = new();

    /// <summary>true = a category rule covers the whole subtree (parent or any ancestor); false = direct parent only.</summary>
    [JsonPropertyName("nestedCategories")]
    public bool NestedCategories { get; set; } = true;

    /// <summary>
    /// true = tpls in config/pool_bundled.json (live tarkov.dev noFlea items) are treated as if listed in
    /// <c>remove</c>. Explicit <c>add</c> entries still win. Default false: it changes what players can trade.
    /// </summary>
    [JsonPropertyName("useLiveBlacklist")]
    public bool UseLiveBlacklist { get; set; } = false;
}
