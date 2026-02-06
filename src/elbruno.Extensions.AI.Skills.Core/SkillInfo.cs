namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Full representation of a skill including properties, body content, and location.
/// </summary>
/// <param name="Properties">Parsed frontmatter properties.</param>
/// <param name="Body">Markdown body content after the frontmatter.</param>
/// <param name="Location">Absolute path to the SKILL.md file.</param>
public record SkillInfo(
    SkillProperties Properties,
    string Body,
    string Location);
