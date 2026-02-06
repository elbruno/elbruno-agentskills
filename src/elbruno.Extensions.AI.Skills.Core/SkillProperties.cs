namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Properties parsed from a skill's SKILL.md frontmatter.
/// </summary>
/// <param name="Name">Skill name in kebab-case (required, max 64 chars).</param>
/// <param name="Description">What the skill does and when to use it (required, max 1024 chars).</param>
/// <param name="License">License for the skill (optional).</param>
/// <param name="Compatibility">Environment requirements (optional, max 500 chars).</param>
/// <param name="AllowedTools">Space-delimited list of pre-approved tools (optional, experimental).</param>
/// <param name="Metadata">Arbitrary key-value pairs for client-specific properties.</param>
public record SkillProperties(
    string Name,
    string Description,
    string? License = null,
    string? Compatibility = null,
    string? AllowedTools = null,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Converts to a dictionary representation, excluding null/empty values.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description
        };

        if (License is not null)
            result["license"] = License;
        if (Compatibility is not null)
            result["compatibility"] = Compatibility;
        if (AllowedTools is not null)
            result["allowed-tools"] = AllowedTools;
        if (Metadata is { Count: > 0 })
            result["metadata"] = Metadata;

        return result;
    }
}
