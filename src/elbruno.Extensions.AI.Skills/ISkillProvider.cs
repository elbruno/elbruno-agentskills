using elbruno.Extensions.AI.Skills.Core;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// Interface for discovering and loading Agent Skills.
/// </summary>
public interface ISkillProvider
{
    /// <summary>
    /// Gets the metadata (name and description) for all discovered skills.
    /// This is a lightweight operation suitable for startup.
    /// </summary>
    IReadOnlyList<SkillProperties> GetSkillMetadata();

    /// <summary>
    /// Gets the full skill information including body and location.
    /// Use this when a skill needs to be activated.
    /// </summary>
    SkillInfo? GetSkill(string skillName);

    /// <summary>
    /// Generates the &lt;available_skills&gt; XML block for agent system prompts.
    /// </summary>
    string GetAvailableSkillsPrompt();

    /// <summary>
    /// Refreshes the list of discovered skills from configured directories.
    /// </summary>
    void Refresh();
}
