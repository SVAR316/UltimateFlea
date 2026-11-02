using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;

namespace UltimateFlea;

public record ModMetadata : IModMetadata, IModBlazorMetadata
{
    public static string AssemblyVersion { get; } =
        typeof(ModMetadata).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "1.3.0";

    public string ModGuid { get; init; } = "com.zloymolodoy.ultimateflea";
    public string Name { get; init; } = "UltimateFlea";
    public string Author { get; init; } = "ZloyMolodoy";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new(AssemblyVersion.Split('+')[0]);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";

    // SPT maps wwwroot to /ultimateflea and shows a card on the server landing page.
    public string? WWWRootUrl { get; init; } = "ultimateflea";
    // Prefer static shell if Blazor host misbehaves; landing card still deep-links into the UI.
    public string? HomePage { get; init; } = "/ultimateflea/app.html";
    public string? HomePageDescription { get; init; } =
        "Flea market settings, economy dashboard, and quick-list controls.";
}
