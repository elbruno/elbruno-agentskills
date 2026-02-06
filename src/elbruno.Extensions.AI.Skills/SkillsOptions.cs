using elbruno.Extensions.AI.Skills.Core;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// Configuration options for Agent Skills integration.
/// </summary>
public class SkillsOptions
{
    /// <summary>
    /// Directories to scan for skill folders containing SKILL.md files.
    /// </summary>
    public List<string> SkillDirectories { get; set; } = [];

    /// <summary>
    /// Whether to automatically discover skills on startup. Default is true.
    /// </summary>
    public bool AutoDiscover { get; set; } = true;

    /// <summary>
    /// Whether to watch skill directories for changes. Default is false.
    /// </summary>
    public bool WatchForChanges { get; set; } = false;

    /// <summary>
    /// Trusted skill names for script execution. Empty means no scripts are allowed.
    /// </summary>
    public HashSet<string> TrustedSkills { get; set; } = [];

    /// <summary>
    /// Whether to require user confirmation before executing scripts. Default is true.
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;
}
