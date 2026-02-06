using System.Collections.Concurrent;
using elbruno.Extensions.AI.Skills.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace elbruno.Extensions.AI.Skills;

/// <summary>
/// Default implementation of <see cref="ISkillProvider"/> that discovers skills from the file system.
/// </summary>
public class FileSystemSkillProvider : ISkillProvider
{
    private readonly SkillsOptions _options;
    private readonly ILogger<FileSystemSkillProvider> _logger;
    private ConcurrentDictionary<string, SkillInfo> _skills = new();

    public FileSystemSkillProvider(IOptions<SkillsOptions> options, ILogger<FileSystemSkillProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.AutoDiscover)
            Refresh();
    }

    /// <inheritdoc />
    public IReadOnlyList<SkillProperties> GetSkillMetadata()
    {
        return _skills.Values.Select(s => s.Properties).ToList();
    }

    /// <inheritdoc />
    public SkillInfo? GetSkill(string skillName)
    {
        return _skills.GetValueOrDefault(skillName);
    }

    /// <inheritdoc />
    public string GetAvailableSkillsPrompt()
    {
        var dirs = _skills.Values.Select(s => Path.GetDirectoryName(s.Location)!).ToList();
        return SkillPromptGenerator.ToPromptXml(dirs);
    }

    /// <inheritdoc />
    public void Refresh()
    {
        var newSkills = new ConcurrentDictionary<string, SkillInfo>();

        foreach (var directory in _options.SkillDirectories)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogWarning("Skill directory does not exist: {Directory}", directory);
                continue;
            }

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                try
                {
                    var skillMd = SkillParser.FindSkillMd(subDir);
                    if (skillMd is null)
                        continue;

                    var skill = SkillParser.ReadSkill(subDir);
                    var errors = SkillValidator.Validate(subDir);

                    if (errors.Count > 0)
                    {
                        _logger.LogWarning("Skill '{SkillDir}' has validation errors: {Errors}",
                            subDir, string.Join("; ", errors));
                        continue;
                    }

                    newSkills[skill.Properties.Name] = skill;
                    _logger.LogDebug("Discovered skill: {SkillName}", skill.Properties.Name);
                }
                catch (SkillException ex)
                {
                    _logger.LogWarning(ex, "Failed to load skill from {SkillDir}", subDir);
                }
            }
        }

        _skills = newSkills;
        _logger.LogInformation("Discovered {Count} skills", _skills.Count);
    }
}
