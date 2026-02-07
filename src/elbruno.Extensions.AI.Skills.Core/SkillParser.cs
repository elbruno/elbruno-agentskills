using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Parses SKILL.md files and extracts skill properties.
/// </summary>
public static class SkillParser
{
    /// <summary>
    /// Finds the SKILL.md file in a skill directory.
    /// Prefers SKILL.md (uppercase) but accepts skill.md (lowercase).
    /// </summary>
    public static string? FindSkillMd(string skillDir)
    {
        foreach (var name in new[] { SkillConstants.SkillFileName, SkillConstants.SkillFileNameLower })
        {
            var path = Path.Combine(skillDir, name);
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    /// <summary>
    /// Parses YAML frontmatter from SKILL.md content.
    /// </summary>
    /// <returns>Tuple of (metadata dictionary, markdown body).</returns>
    public static (Dictionary<string, object> Metadata, string Body) ParseFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            throw new SkillParseException("SKILL.md must start with YAML frontmatter (---)");

        var parts = content.Split("---", 3);
        if (parts.Length < 3)
            throw new SkillParseException("SKILL.md frontmatter not properly closed with ---");

        var frontmatterStr = parts[1];
        var body = parts[2].Trim();

        Dictionary<string, object> metadata;
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            metadata = deserializer.Deserialize<Dictionary<string, object>>(frontmatterStr)
                ?? throw new SkillParseException("SKILL.md frontmatter must be a YAML mapping");
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new SkillParseException($"Invalid YAML in frontmatter: {ex.Message}", ex);
        }

        // Normalize metadata values to strings
        if (metadata.TryGetValue("metadata", out var metaValue) && metaValue is Dictionary<object, object> rawMeta)
        {
            metadata["metadata"] = rawMeta.ToDictionary(
                k => k.Key.ToString()!,
                v => (object)v.Value.ToString()!);
        }

        return (metadata, body);
    }

    /// <summary>
    /// Reads skill properties from a SKILL.md file's frontmatter.
    /// Does NOT perform full validation — use <see cref="SkillValidator.Validate"/> for that.
    /// </summary>
    public static SkillProperties ReadProperties(string skillDir)
    {
        var skillMd = FindSkillMd(skillDir)
            ?? throw new SkillParseException($"SKILL.md not found in {skillDir}");

        var content = File.ReadAllText(skillMd);
        var (metadata, _) = ParseFrontmatter(content);

        return ExtractProperties(metadata);
    }

    /// <summary>
    /// Reads the full skill including properties, body, location, and bundled resources.
    /// </summary>
    public static SkillInfo ReadSkill(string skillDir)
    {
        var skillMd = FindSkillMd(skillDir)
            ?? throw new SkillParseException($"SKILL.md not found in {skillDir}");

        var content = File.ReadAllText(skillMd);
        var (metadata, body) = ParseFrontmatter(content);
        var properties = ExtractProperties(metadata);

        var scripts = EnumerateResources(skillDir, SkillConstants.ScriptsDirectory, SkillResourceType.Script);
        var references = EnumerateResources(skillDir, SkillConstants.ReferencesDirectory, SkillResourceType.Reference);
        var assets = EnumerateResources(skillDir, SkillConstants.AssetsDirectory, SkillResourceType.Asset);

        return new SkillInfo(properties, body, Path.GetFullPath(skillMd), scripts, references, assets);
    }

    /// <summary>
    /// Enumerates files in a skill subdirectory and returns them as <see cref="SkillResource"/> descriptors.
    /// Only scans one level deep (no recursive enumeration).
    /// </summary>
    private static IReadOnlyList<SkillResource> EnumerateResources(
        string skillDir, string subDirectory, SkillResourceType resourceType)
    {
        var dirPath = Path.Combine(skillDir, subDirectory);
        if (!Directory.Exists(dirPath))
            return Array.Empty<SkillResource>();

        return Directory.EnumerateFiles(dirPath)
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(absPath =>
            {
                var fileName = Path.GetFileName(absPath);
                var relativePath = Path.Combine(subDirectory, fileName).Replace('\\', '/');
                return new SkillResource(
                    resourceType,
                    relativePath,
                    Path.GetFullPath(absPath),
                    fileName,
                    Path.GetExtension(absPath));
            })
            .ToList();
    }

    /// <summary>
    /// Known resource subdirectory names mapped to their <see cref="SkillResourceType"/>.
    /// </summary>
    private static readonly Dictionary<string, SkillResourceType> ResourceDirectoryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [SkillConstants.ScriptsDirectory] = SkillResourceType.Script,
        [SkillConstants.ReferencesDirectory] = SkillResourceType.Reference,
        [SkillConstants.AssetsDirectory] = SkillResourceType.Asset,
    };

    /// <summary>
    /// Regex matching Markdown links: [text](path) and inline code: `path`
    /// where path starts with scripts/, references/, or assets/.
    /// </summary>
    private static readonly Regex FileReferencePattern = new(
        @"(?:\[[^\]]*\]\(|`)(?<path>(?:scripts|references|assets)/[^\)`]+)[`\)]",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses Markdown links and inline code references in the skill body that match
    /// relative paths to <c>scripts/</c>, <c>references/</c>, or <c>assets/</c> subdirectories.
    /// Only returns references to files that actually exist on disk.
    /// </summary>
    /// <param name="body">The Markdown body content of a SKILL.md file.</param>
    /// <param name="skillDir">Absolute path to the skill directory.</param>
    /// <returns>List of <see cref="SkillResource"/> descriptors for referenced files that exist.</returns>
    public static IReadOnlyList<SkillResource> ResolveFileReferences(string body, string skillDir)
    {
        var results = new List<SkillResource>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in FileReferencePattern.Matches(body))
        {
            var relativePath = match.Groups["path"].Value.Trim();

            if (!seen.Add(relativePath))
                continue;

            var absPath = ResolveRelativePath(skillDir, relativePath);

            if (!File.Exists(absPath))
                continue;

            // Determine resource type from the first path segment
            var firstSegment = relativePath.Split('/')[0];
            if (!ResourceDirectoryTypes.TryGetValue(firstSegment, out var resourceType))
                continue;

            var fileName = Path.GetFileName(absPath);
            results.Add(new SkillResource(
                resourceType,
                relativePath,
                absPath,
                fileName,
                Path.GetExtension(absPath)));
        }

        return results;
    }

    /// <summary>
    /// Reads the content of a resource file within a skill directory.
    /// Validates that the relative path stays within the skill directory (prevents path traversal).
    /// </summary>
    /// <param name="skillDir">Absolute path to the skill directory.</param>
    /// <param name="relativePath">Relative path from the skill root, e.g. "scripts/extract.py".</param>
    /// <returns>The file content as a string.</returns>
    /// <exception cref="SkillParseException">
    /// Thrown if the file does not exist or if the resolved path escapes the skill directory.
    /// </exception>
    public static string ReadResource(string skillDir, string relativePath)
    {
        var fullSkillDir = Path.GetFullPath(skillDir);
        var resolvedPath = ResolveRelativePath(fullSkillDir, relativePath);

        // Ensure the resolved path is within the skill directory (prevent path traversal).
        // Uses OrdinalIgnoreCase to handle case-insensitive file systems (Windows, macOS).
        if (!resolvedPath.StartsWith(fullSkillDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !resolvedPath.Equals(fullSkillDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new SkillParseException(
                $"Resource path '{relativePath}' escapes the skill directory '{skillDir}'");
        }

        if (!File.Exists(resolvedPath))
        {
            throw new SkillParseException(
                $"Resource file not found: '{relativePath}' in skill directory '{skillDir}'");
        }

        return File.ReadAllText(resolvedPath);
    }

    /// <summary>
    /// Resolves a forward-slash relative path to an absolute path within a skill directory.
    /// </summary>
    private static string ResolveRelativePath(string skillDir, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(skillDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static SkillProperties ExtractProperties(Dictionary<string, object> metadata)
    {
        if (!metadata.TryGetValue("name", out var nameObj))
            throw new SkillValidationException("Missing required field in frontmatter: name");
        if (!metadata.TryGetValue("description", out var descObj))
            throw new SkillValidationException("Missing required field in frontmatter: description");

        var name = nameObj?.ToString()?.Trim()
            ?? throw new SkillValidationException("Field 'name' must be a non-empty string");
        var description = descObj?.ToString()?.Trim()
            ?? throw new SkillValidationException("Field 'description' must be a non-empty string");

        if (string.IsNullOrWhiteSpace(name))
            throw new SkillValidationException("Field 'name' must be a non-empty string");
        if (string.IsNullOrWhiteSpace(description))
            throw new SkillValidationException("Field 'description' must be a non-empty string");

        IReadOnlyDictionary<string, string>? metadataDict = null;
        if (metadata.TryGetValue("metadata", out var metaVal))
        {
            metadataDict = metaVal switch
            {
                Dictionary<string, object> dict => dict.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? ""),
                Dictionary<object, object> rawDict => rawDict.ToDictionary(k => k.Key.ToString()!, v => v.Value?.ToString() ?? ""),
                _ => null
            };
        }

        return new SkillProperties(
            Name: name,
            Description: description,
            License: metadata.GetValueOrDefault("license")?.ToString(),
            Compatibility: metadata.GetValueOrDefault("compatibility")?.ToString(),
            AllowedTools: metadata.GetValueOrDefault("allowed-tools")?.ToString(),
            Metadata: metadataDict);
    }
}
