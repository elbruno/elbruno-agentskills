namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Constants defined by the Agent Skills specification.
/// </summary>
public static class SkillConstants
{
    public const int MaxSkillNameLength = 64;
    public const int MaxDescriptionLength = 1024;
    public const int MaxCompatibilityLength = 500;
    public const string SkillFileName = "SKILL.md";
    public const string SkillFileNameLower = "skill.md";

    /// <summary>
    /// Allowed top-level fields in SKILL.md frontmatter.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "name",
        "description",
        "license",
        "allowed-tools",
        "metadata",
        "compatibility"
    };
}
