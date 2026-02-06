using System.Net;

namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Generates &lt;available_skills&gt; XML blocks for agent system prompts.
/// </summary>
public static class SkillPromptGenerator
{
    /// <summary>
    /// Generates the &lt;available_skills&gt; XML block for inclusion in agent prompts.
    /// This format is recommended by Anthropic for Claude models.
    /// </summary>
    /// <param name="skillDirs">List of paths to skill directories.</param>
    /// <returns>XML string with the &lt;available_skills&gt; block.</returns>
    public static string ToPromptXml(IEnumerable<string> skillDirs)
    {
        var dirs = skillDirs.ToList();
        if (dirs.Count == 0)
            return "<available_skills>\n</available_skills>";

        var lines = new List<string> { "<available_skills>" };

        foreach (var skillDir in dirs)
        {
            var fullPath = Path.GetFullPath(skillDir);
            var props = SkillParser.ReadProperties(fullPath);
            var skillMdPath = SkillParser.FindSkillMd(fullPath);

            lines.Add("<skill>");
            lines.Add("<name>");
            lines.Add(WebUtility.HtmlEncode(props.Name));
            lines.Add("</name>");
            lines.Add("<description>");
            lines.Add(WebUtility.HtmlEncode(props.Description));
            lines.Add("</description>");
            lines.Add("<location>");
            lines.Add(skillMdPath!);
            lines.Add("</location>");
            lines.Add("</skill>");
        }

        lines.Add("</available_skills>");
        return string.Join("\n", lines);
    }
}
