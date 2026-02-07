using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Validates skill directories and metadata against the Agent Skills specification.
/// </summary>
public static class SkillValidator
{
    /// <summary>
    /// Prefix used to distinguish warnings from errors in validation results.
    /// Warnings are non-blocking issues; errors indicate spec violations.
    /// </summary>
    public const string WarningPrefix = "Warning: ";

    /// <summary>
    /// Validates a skill directory.
    /// </summary>
    /// <returns>List of validation messages. Messages prefixed with "Warning: " are non-blocking.
    /// An empty list means the skill is fully valid with no warnings.</returns>
    public static IReadOnlyList<string> Validate(string skillDir)
    {
        if (!Directory.Exists(skillDir))
            return [$"Path does not exist: {skillDir}"];

        var skillMd = SkillParser.FindSkillMd(skillDir);
        if (skillMd is null)
            return ["Missing required file: SKILL.md"];

        Dictionary<string, object> metadata;
        string body;
        try
        {
            var content = File.ReadAllText(skillMd);
            (metadata, body) = SkillParser.ParseFrontmatter(content);
        }
        catch (SkillParseException ex)
        {
            return [ex.Message];
        }

        var errors = new List<string>(ValidateMetadata(metadata, skillDir));
        errors.AddRange(ValidateSubDirectories(skillDir));
        errors.AddRange(ValidateFileReferences(body, skillDir));

        return errors;
    }

    /// <summary>
    /// Validates parsed skill metadata.
    /// </summary>
    public static IReadOnlyList<string> ValidateMetadata(Dictionary<string, object> metadata, string? skillDir = null)
    {
        var errors = new List<string>();

        errors.AddRange(ValidateAllowedFields(metadata));

        if (!metadata.TryGetValue("name", out var nameObj))
        {
            errors.Add("Missing required field in frontmatter: name");
        }
        else
        {
            errors.AddRange(ValidateName(nameObj?.ToString() ?? "", skillDir));
        }

        if (!metadata.TryGetValue("description", out var descObj))
        {
            errors.Add("Missing required field in frontmatter: description");
        }
        else
        {
            errors.AddRange(ValidateDescription(descObj?.ToString() ?? ""));
        }

        if (metadata.TryGetValue("compatibility", out var compObj))
        {
            errors.AddRange(ValidateCompatibility(compObj?.ToString() ?? ""));
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateName(string name, string? skillDir)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Field 'name' must be a non-empty string");
            return errors;
        }

        name = name.Normalize(NormalizationForm.FormKC).Trim();

        if (name.Length > SkillConstants.MaxSkillNameLength)
            errors.Add($"Skill name '{name}' exceeds {SkillConstants.MaxSkillNameLength} character limit ({name.Length} chars)");

        if (name != name.ToLowerInvariant())
            errors.Add($"Skill name '{name}' must be lowercase");

        if (name.StartsWith('-') || name.EndsWith('-'))
            errors.Add("Skill name cannot start or end with a hyphen");

        if (name.Contains("--"))
            errors.Add("Skill name cannot contain consecutive hyphens");

        if (!name.All(c => char.IsLetterOrDigit(c) || c == '-'))
            errors.Add($"Skill name '{name}' contains invalid characters. Only letters, digits, and hyphens are allowed.");

        if (skillDir is not null)
        {
            var dirName = Path.GetFileName(skillDir).Normalize(NormalizationForm.FormKC);
            if (dirName != name)
                errors.Add($"Directory name '{Path.GetFileName(skillDir)}' must match skill name '{name}'");
        }

        return errors;
    }

    private static IReadOnlyList<string> ValidateDescription(string description)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(description))
        {
            errors.Add("Field 'description' must be a non-empty string");
            return errors;
        }

        if (description.Length > SkillConstants.MaxDescriptionLength)
            errors.Add($"Description exceeds {SkillConstants.MaxDescriptionLength} character limit ({description.Length} chars)");

        return errors;
    }

    private static IReadOnlyList<string> ValidateCompatibility(string compatibility)
    {
        var errors = new List<string>();

        if (compatibility.Length > SkillConstants.MaxCompatibilityLength)
            errors.Add($"Compatibility exceeds {SkillConstants.MaxCompatibilityLength} character limit ({compatibility.Length} chars)");

        return errors;
    }

    private static IReadOnlyList<string> ValidateAllowedFields(Dictionary<string, object> metadata)
    {
        var errors = new List<string>();
        var extraFields = metadata.Keys.Where(k => !SkillConstants.AllowedFields.Contains(k)).OrderBy(k => k).ToList();

        if (extraFields.Count > 0)
            errors.Add($"Unexpected fields in frontmatter: {string.Join(", ", extraFields)}. Only [{string.Join(", ", SkillConstants.AllowedFields.OrderBy(f => f))}] are allowed.");

        return errors;
    }

    /// <summary>
    /// Regex matching Markdown links and inline code references to resource subdirectories.
    /// </summary>
    private static readonly Regex FileReferencePattern = new(
        @"(?:\[[^\]]*\]\(|`)(?<path>(?:scripts|references|assets)/[^\)`]+)[`\)]",
        RegexOptions.Compiled);

    /// <summary>
    /// Warns if <c>scripts/</c>, <c>references/</c>, or <c>assets/</c> subdirectories exist but contain zero files.
    /// </summary>
    private static IReadOnlyList<string> ValidateSubDirectories(string skillDir)
    {
        var warnings = new List<string>();

        foreach (var subDir in new[] { SkillConstants.ScriptsDirectory, SkillConstants.ReferencesDirectory, SkillConstants.AssetsDirectory })
        {
            var dirPath = Path.Combine(skillDir, subDir);
            if (Directory.Exists(dirPath) && !Directory.EnumerateFiles(dirPath).Any())
            {
                warnings.Add($"{WarningPrefix}Directory '{subDir}/' exists but contains no files");
            }
        }

        return warnings;
    }

    /// <summary>
    /// Warns if the body references files via Markdown links or inline code that don't exist on disk.
    /// Also validates that file references don't escape the skill directory via path traversal.
    /// </summary>
    private static IReadOnlyList<string> ValidateFileReferences(string body, string skillDir)
    {
        var messages = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var fullSkillDir = Path.GetFullPath(skillDir) + Path.DirectorySeparatorChar;

        foreach (Match match in FileReferencePattern.Matches(body))
        {
            var relativePath = match.Groups["path"].Value.Trim();

            if (!seen.Add(relativePath))
                continue;

            var resolvedPath = Path.GetFullPath(Path.Combine(
                skillDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

            // Always validate containment — catches path traversal regardless of `..` presence
            if (!resolvedPath.StartsWith(fullSkillDir, StringComparison.OrdinalIgnoreCase))
            {
                messages.Add($"File reference '{relativePath}' escapes the skill directory");
                continue;
            }

            // Check if referenced file exists
            if (!File.Exists(resolvedPath))
            {
                messages.Add($"{WarningPrefix}Body references '{relativePath}' but the file does not exist");
            }
        }

        return messages;
    }
}
