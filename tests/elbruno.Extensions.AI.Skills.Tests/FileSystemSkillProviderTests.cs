using elbruno.Extensions.AI.Skills.Core;

namespace elbruno.Extensions.AI.Skills.Tests;

public class FileSystemSkillProviderTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemSkillProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-provider-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateSkillDir(string name, string description = "A test skill.")
    {
        var skillDir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {description}\n---\n# {name}\nInstructions here.");
        return skillDir;
    }

    private FileSystemSkillProvider CreateProvider(params string[] dirs)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SkillsOptions
        {
            SkillDirectories = [.. dirs],
            AutoDiscover = true
        });
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemSkillProvider>();
        return new FileSystemSkillProvider(options, logger);
    }

    [Fact]
    public void GetSkillMetadata_ReturnsDiscoveredSkills()
    {
        CreateSkillDir("skill-a", "First skill");
        CreateSkillDir("skill-b", "Second skill");

        var provider = CreateProvider(_tempDir);
        var metadata = provider.GetSkillMetadata();

        Assert.Equal(2, metadata.Count);
        Assert.Contains(metadata, m => m.Name == "skill-a");
        Assert.Contains(metadata, m => m.Name == "skill-b");
    }

    [Fact]
    public void GetSkill_ReturnsCorrectSkill()
    {
        CreateSkillDir("my-skill", "A skill for testing.");
        var provider = CreateProvider(_tempDir);

        var skill = provider.GetSkill("my-skill");

        Assert.NotNull(skill);
        Assert.Equal("my-skill", skill.Properties.Name);
        Assert.Contains("Instructions here", skill.Body);
    }

    [Fact]
    public void GetSkill_ReturnsNull_WhenNotFound()
    {
        var provider = CreateProvider(_tempDir);
        Assert.Null(provider.GetSkill("nonexistent"));
    }

    [Fact]
    public void GetAvailableSkillsPrompt_GeneratesXml()
    {
        CreateSkillDir("prompt-skill", "Generate prompts.");
        var provider = CreateProvider(_tempDir);

        var prompt = provider.GetAvailableSkillsPrompt();

        Assert.Contains("<available_skills>", prompt);
        Assert.Contains("prompt-skill", prompt);
        Assert.Contains("Generate prompts.", prompt);
    }

    [Fact]
    public void Refresh_UpdatesSkillList()
    {
        var provider = CreateProvider(_tempDir);
        Assert.Empty(provider.GetSkillMetadata());

        CreateSkillDir("new-skill", "Freshly added.");
        provider.Refresh();

        Assert.Single(provider.GetSkillMetadata());
    }

    [Fact]
    public void Provider_SkipsInvalidSkills()
    {
        CreateSkillDir("valid-skill", "A valid one.");
        // Create an invalid skill (name mismatch)
        var invalidDir = Path.Combine(_tempDir, "wrong-dir");
        Directory.CreateDirectory(invalidDir);
        File.WriteAllText(Path.Combine(invalidDir, "SKILL.md"),
            "---\nname: correct-name\ndescription: test\n---\nBody");

        var provider = CreateProvider(_tempDir);
        var metadata = provider.GetSkillMetadata();

        Assert.Single(metadata);
        Assert.Equal("valid-skill", metadata[0].Name);
    }

    [Fact]
    public void Provider_HandlesEmptyDirectories()
    {
        var provider = CreateProvider(_tempDir);
        Assert.Empty(provider.GetSkillMetadata());
        Assert.Equal("<available_skills>\n</available_skills>", provider.GetAvailableSkillsPrompt());
    }
}
