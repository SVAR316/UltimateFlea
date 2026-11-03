using System.Text.Json;

namespace UltimateFlea.Presets;

/// <summary>Bilingual text; serialised as { "en": ..., "ru": ... } by the camelCase web options.</summary>
public record LocalizedText(string En, string Ru)
{
    /// <summary>Both languages joined with a space; either side may be null.</summary>
    public static LocalizedText? Join(LocalizedText? a, LocalizedText? b) =>
        a is null ? b : b is null ? a : new($"{a.En} {b.En}", $"{a.Ru} {b.Ru}");
}

/// <summary>
/// One catalogue entry. <see cref="Configs"/> is null for presets whose content depends on the
/// running server (see <see cref="PresetKind.Dynamic"/>); those are resolved at request time.
/// </summary>
public sealed record PresetDefinition(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    string[] Tags,
    string[] Requires,
    PresetKind Kind,
    IReadOnlyDictionary<string, JsonElement>? Configs)
{
    /// <summary>Config names written by the preset. Dynamic presets declare theirs explicitly.</summary>
    public string[] Targets { get; init; } = Configs?.Keys.ToArray() ?? [];
}

public enum PresetKind
{
    /// <summary>Fixed JSON, identical on every install.</summary>
    Static,

    /// <summary>Needs the live item database and/or the mods folder to compute its content.</summary>
    Dynamic,
}

/// <summary>Element of the list endpoint. Property names are the wire contract.</summary>
public sealed record PresetSummary(
    string Id,
    LocalizedText Name,
    LocalizedText Description,
    string[] Targets,
    string[] Tags,
    string[] Requires,
    bool Available,
    LocalizedText? UnavailableReason);

public sealed record PresetListResponse(IReadOnlyList<PresetSummary> Presets);

public sealed record PresetDetailResponse(string Id, IReadOnlyDictionary<string, JsonElement> Configs);

/// <summary>Outcome of applying a preset. <see cref="Error"/> set means the apply was refused or incomplete.</summary>
public sealed record PresetApplyResult(
    IReadOnlyList<string> Applied,
    bool RestartRecommended,
    LocalizedText? Warning,
    LocalizedText? Error,
    int HttpStatus);
