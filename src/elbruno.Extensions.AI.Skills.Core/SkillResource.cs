namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Identifies the type of a bundled resource within a skill directory.
/// </summary>
public enum SkillResourceType
{
    Script,
    Reference,
    Asset
}

/// <summary>
/// Lightweight descriptor for a file bundled in a skill's scripts/, references/, or assets/ directory.
/// Content is loaded on demand (progressive disclosure Level 3).
/// </summary>
/// <param name="Type">The resource category.</param>
/// <param name="RelativePath">Path relative to the skill root, e.g. "scripts/extract.py".</param>
/// <param name="AbsolutePath">Fully resolved path on disk.</param>
/// <param name="FileName">File name including extension.</param>
/// <param name="Extension">File extension including the dot, e.g. ".py".</param>
public record SkillResource(
    SkillResourceType Type,
    string RelativePath,
    string AbsolutePath,
    string FileName,
    string Extension);
