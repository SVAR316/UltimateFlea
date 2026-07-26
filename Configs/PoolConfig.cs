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
}
