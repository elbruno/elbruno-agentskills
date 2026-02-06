using System.Globalization;
using System.Text;

namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Validates skill directories and metadata against the Agent Skills specification.
/// </summary>
public static class SkillValidator
{
    /// <summary>
    /// Validates a skill directory.
    /// </summary>
    /// <returns>List of validation error messages. Empty list means valid.</returns>
    public static IReadOnlyList<string> Validate(string skillDir)
    {
        if (!Directory.Exists(skillDir))
            return [$"Path does not exist: {skillDir}"];

        var skillMd = SkillParser.FindSkillMd(skillDir);
        if (skillMd is null)
            return ["Missing required file: SKILL.md"];

        Dictionary<string, object> metadata;
        try
        {
            var content = File.ReadAllText(skillMd);
            (metadata, _) = SkillParser.ParseFrontmatter(content);
        }
        catch (SkillParseException ex)
        {
            return [ex.Message];
        }

        return ValidateMetadata(metadata, skillDir);
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
}
