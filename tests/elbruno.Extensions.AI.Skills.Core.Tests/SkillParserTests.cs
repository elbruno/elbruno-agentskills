namespace elbruno.Extensions.AI.Skills.Core.Tests;

public class SkillParserTests : IDisposable
{
    private readonly string _tempDir;

    public SkillParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateSkillDir(string name, string content)
    {
        var skillDir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), content);
        return skillDir;
    }

    [Fact]
    public void FindSkillMd_ReturnsPath_WhenSkillMdExists()
    {
        var dir = CreateSkillDir("test-skill", "---\nname: test-skill\ndescription: test\n---\n");
        var result = SkillParser.FindSkillMd(dir);
        Assert.NotNull(result);
        Assert.EndsWith("SKILL.md", result);
    }

    [Fact]
    public void FindSkillMd_ReturnsNull_WhenNoSkillMd()
    {
        var dir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(dir);
        Assert.Null(SkillParser.FindSkillMd(dir));
    }

    [Fact]
    public void ParseFrontmatter_ParsesValidContent()
    {
        var content = "---\nname: my-skill\ndescription: Does something cool\n---\n# Body\nSome instructions.";
        var (metadata, body) = SkillParser.ParseFrontmatter(content);

        Assert.Equal("my-skill", metadata["name"]);
        Assert.Equal("Does something cool", metadata["description"]);
        Assert.Equal("# Body\nSome instructions.", body);
    }

    [Fact]
    public void ParseFrontmatter_ThrowsOnMissingFrontmatter()
    {
        Assert.Throws<SkillParseException>(() => SkillParser.ParseFrontmatter("No frontmatter here"));
    }

    [Fact]
    public void ParseFrontmatter_ThrowsOnUnclosedFrontmatter()
    {
        Assert.Throws<SkillParseException>(() => SkillParser.ParseFrontmatter("---\nname: test\n"));
    }

    [Fact]
    public void ReadProperties_ReturnsCorrectProperties()
    {
        var dir = CreateSkillDir("pdf-processing", """
            ---
            name: pdf-processing
            description: Extract text and tables from PDF files.
            license: Apache-2.0
            compatibility: Requires Python 3.10+
            ---
            # PDF Processing
            """);

        var props = SkillParser.ReadProperties(dir);

        Assert.Equal("pdf-processing", props.Name);
        Assert.Equal("Extract text and tables from PDF files.", props.Description);
        Assert.Equal("Apache-2.0", props.License);
        Assert.Equal("Requires Python 3.10+", props.Compatibility);
    }

    [Fact]
    public void ReadProperties_HandlesMetadata()
    {
        var dir = CreateSkillDir("meta-skill", """
            ---
            name: meta-skill
            description: Skill with metadata.
            metadata:
              author: example-org
              version: "1.0"
            ---
            Body
            """);

        var props = SkillParser.ReadProperties(dir);

        Assert.NotNull(props.Metadata);
        Assert.Equal("example-org", props.Metadata["author"]);
        Assert.Equal("1.0", props.Metadata["version"]);
    }

    [Fact]
    public void ReadProperties_ThrowsOnMissingName()
    {
        var dir = CreateSkillDir("no-name", "---\ndescription: test\n---\nBody");
        Assert.Throws<SkillValidationException>(() => SkillParser.ReadProperties(dir));
    }

    [Fact]
    public void ReadProperties_ThrowsOnMissingDescription()
    {
        var dir = CreateSkillDir("no-desc", "---\nname: no-desc\n---\nBody");
        Assert.Throws<SkillValidationException>(() => SkillParser.ReadProperties(dir));
    }

    [Fact]
    public void ReadProperties_ThrowsOnMissingFile()
    {
        var dir = Path.Combine(_tempDir, "nonexistent");
        Directory.CreateDirectory(dir);
        Assert.Throws<SkillParseException>(() => SkillParser.ReadProperties(dir));
    }

    [Fact]
    public void ReadSkill_ReturnsFullSkillInfo()
    {
        var dir = CreateSkillDir("full-skill", """
            ---
            name: full-skill
            description: A complete skill.
            ---
            # Instructions
            Step 1: Do something.
            """);

        var skill = SkillParser.ReadSkill(dir);

        Assert.Equal("full-skill", skill.Properties.Name);
        Assert.Contains("Step 1", skill.Body);
        Assert.Contains("SKILL.md", skill.Location);
    }

    [Fact]
    public void ReadProperties_HandlesAllowedTools()
    {
        var dir = CreateSkillDir("tool-skill", """
            ---
            name: tool-skill
            description: Skill with tools.
            allowed-tools: Bash(git:*) Read
            ---
            Body
            """);

        var props = SkillParser.ReadProperties(dir);
        Assert.Equal("Bash(git:*) Read", props.AllowedTools);
    }
}
