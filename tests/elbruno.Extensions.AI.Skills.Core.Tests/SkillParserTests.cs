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

    [Fact]
    public void ReadSkill_ReturnsEmptyResourceLists_WhenNoSubDirectories()
    {
        var dir = CreateSkillDir("no-resources", """
            ---
            name: no-resources
            description: Skill without resource directories.
            ---
            Body
            """);

        var skill = SkillParser.ReadSkill(dir);

        Assert.Empty(skill.Scripts);
        Assert.Empty(skill.References);
        Assert.Empty(skill.Assets);
    }

    [Fact]
    public void ReadSkill_PopulatesScripts_WhenScriptsDirExists()
    {
        var dir = CreateSkillDir("script-skill", """
            ---
            name: script-skill
            description: Skill with scripts.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "extract.py"), "print('hello')");
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hello");

        var skill = SkillParser.ReadSkill(dir);

        Assert.Equal(2, skill.Scripts.Count);
        Assert.Contains(skill.Scripts, s => s.FileName == "extract.py" && s.Extension == ".py");
        Assert.Contains(skill.Scripts, s => s.FileName == "run.sh" && s.Extension == ".sh");
        Assert.All(skill.Scripts, s =>
        {
            Assert.Equal(SkillResourceType.Script, s.Type);
            Assert.StartsWith("scripts/", s.RelativePath);
        });
    }

    [Fact]
    public void ReadSkill_PopulatesReferences_WhenReferencesDirExists()
    {
        var dir = CreateSkillDir("ref-skill", """
            ---
            name: ref-skill
            description: Skill with references.
            ---
            Body
            """);
        var refsDir = Path.Combine(dir, "references");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(refsDir, "REFERENCE.md"), "# Ref");

        var skill = SkillParser.ReadSkill(dir);

        Assert.Single(skill.References);
        Assert.Equal("REFERENCE.md", skill.References[0].FileName);
        Assert.Equal(SkillResourceType.Reference, skill.References[0].Type);
        Assert.Equal("references/REFERENCE.md", skill.References[0].RelativePath);
    }

    [Fact]
    public void ReadSkill_PopulatesAssets_WhenAssetsDirExists()
    {
        var dir = CreateSkillDir("asset-skill", """
            ---
            name: asset-skill
            description: Skill with assets.
            ---
            Body
            """);
        var assetsDir = Path.Combine(dir, "assets");
        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(Path.Combine(assetsDir, "template.docx"), "content");
        File.WriteAllText(Path.Combine(assetsDir, "schema.json"), "{}");

        var skill = SkillParser.ReadSkill(dir);

        Assert.Equal(2, skill.Assets.Count);
        Assert.All(skill.Assets, a => Assert.Equal(SkillResourceType.Asset, a.Type));
        Assert.Contains(skill.Assets, a => a.FileName == "template.docx");
        Assert.Contains(skill.Assets, a => a.FileName == "schema.json");
    }

    [Fact]
    public void ReadSkill_DoesNotScanSubDirectoriesRecursively()
    {
        var dir = CreateSkillDir("flat-skill", """
            ---
            name: flat-skill
            description: Only one level deep.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "top-level.py"), "# top");
        var nested = Path.Combine(scriptsDir, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "deep.py"), "# nested");

        var skill = SkillParser.ReadSkill(dir);

        Assert.Single(skill.Scripts);
        Assert.Equal("top-level.py", skill.Scripts[0].FileName);
    }

    [Fact]
    public void ReadSkill_ResourceAbsolutePath_IsFullyResolved()
    {
        var dir = CreateSkillDir("abs-path-skill", """
            ---
            name: abs-path-skill
            description: Check absolute paths.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hi");

        var skill = SkillParser.ReadSkill(dir);

        Assert.Single(skill.Scripts);
        Assert.True(Path.IsPathRooted(skill.Scripts[0].AbsolutePath));
    }

    [Fact]
    public void SkillInfo_BackwardCompatibleConstructor_SetsEmptyLists()
    {
        var props = new SkillProperties("test", "test description");
        var info = new SkillInfo(props, "body", "/path/SKILL.md");

        Assert.Empty(info.Scripts);
        Assert.Empty(info.References);
        Assert.Empty(info.Assets);
    }
}
