namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Full representation of a skill including properties, body content, location, and bundled resources.
/// </summary>
/// <param name="Properties">Parsed frontmatter properties.</param>
/// <param name="Body">Markdown body content after the frontmatter.</param>
/// <param name="Location">Absolute path to the SKILL.md file.</param>
/// <param name="Scripts">Script resources found in the scripts/ subdirectory.</param>
/// <param name="References">Reference resources found in the references/ subdirectory.</param>
/// <param name="Assets">Asset resources found in the assets/ subdirectory.</param>
public record SkillInfo(
    SkillProperties Properties,
    string Body,
    string Location,
    IReadOnlyList<SkillResource> Scripts,
    IReadOnlyList<SkillResource> References,
    IReadOnlyList<SkillResource> Assets)
{
    /// <summary>
    /// Creates a SkillInfo without resource lists (backward-compatible).
    /// </summary>
    public SkillInfo(SkillProperties Properties, string Body, string Location)
        : this(Properties, Body, Location,
              Array.Empty<SkillResource>(),
              Array.Empty<SkillResource>(),
              Array.Empty<SkillResource>()) { }
}
