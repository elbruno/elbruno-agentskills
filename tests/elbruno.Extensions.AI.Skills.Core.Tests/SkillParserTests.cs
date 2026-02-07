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

    // --- ResolveFileReferences tests ---

    [Fact]
    public void ResolveFileReferences_FindsMarkdownLinks()
    {
        var dir = CreateSkillDir("ref-link-skill", """
            ---
            name: ref-link-skill
            description: Skill with markdown links.
            ---
            See [the script](scripts/extract.py) for details.
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "extract.py"), "print('hello')");

        var refs = SkillParser.ResolveFileReferences("See [the script](scripts/extract.py) for details.", dir);

        Assert.Single(refs);
        Assert.Equal("scripts/extract.py", refs[0].RelativePath);
        Assert.Equal("extract.py", refs[0].FileName);
        Assert.Equal(SkillResourceType.Script, refs[0].Type);
    }

    [Fact]
    public void ResolveFileReferences_FindsInlineCodeReferences()
    {
        var dir = CreateSkillDir("ref-code-skill", """
            ---
            name: ref-code-skill
            description: Skill with inline code refs.
            ---
            Use `references/REFERENCE.md` for more info.
            """);
        var refsDir = Path.Combine(dir, "references");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(refsDir, "REFERENCE.md"), "# Ref");

        var refs = SkillParser.ResolveFileReferences("Use `references/REFERENCE.md` for more info.", dir);

        Assert.Single(refs);
        Assert.Equal("references/REFERENCE.md", refs[0].RelativePath);
        Assert.Equal(SkillResourceType.Reference, refs[0].Type);
    }

    [Fact]
    public void ResolveFileReferences_FindsMultipleReferences()
    {
        var dir = CreateSkillDir("multi-ref-skill", """
            ---
            name: multi-ref-skill
            description: Skill with multiple refs.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hi");
        var assetsDir = Path.Combine(dir, "assets");
        Directory.CreateDirectory(assetsDir);
        File.WriteAllText(Path.Combine(assetsDir, "template.md"), "# Template");

        var body = "Run [this](scripts/run.sh) and use `assets/template.md`.";
        var refs = SkillParser.ResolveFileReferences(body, dir);

        Assert.Equal(2, refs.Count);
        Assert.Contains(refs, r => r.RelativePath == "scripts/run.sh" && r.Type == SkillResourceType.Script);
        Assert.Contains(refs, r => r.RelativePath == "assets/template.md" && r.Type == SkillResourceType.Asset);
    }

    [Fact]
    public void ResolveFileReferences_IgnoresNonExistentFiles()
    {
        var dir = CreateSkillDir("missing-ref-skill", """
            ---
            name: missing-ref-skill
            description: Skill referencing missing file.
            ---
            Body
            """);

        var refs = SkillParser.ResolveFileReferences("See [missing](scripts/nonexistent.py).", dir);

        Assert.Empty(refs);
    }

    [Fact]
    public void ResolveFileReferences_DeduplicatesReferences()
    {
        var dir = CreateSkillDir("dedup-ref-skill", """
            ---
            name: dedup-ref-skill
            description: Skill with duplicate refs.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hi");

        var body = "Use [run](scripts/run.sh) and also `scripts/run.sh`.";
        var refs = SkillParser.ResolveFileReferences(body, dir);

        Assert.Single(refs);
    }

    [Fact]
    public void ResolveFileReferences_IgnoresNonResourcePaths()
    {
        var dir = CreateSkillDir("non-resource-skill", """
            ---
            name: non-resource-skill
            description: Skill with non-resource links.
            ---
            Body
            """);

        var body = "See [docs](docs/README.md) and [link](https://example.com).";
        var refs = SkillParser.ResolveFileReferences(body, dir);

        Assert.Empty(refs);
    }

    [Fact]
    public void ResolveFileReferences_ReturnsEmptyForBodyWithNoLinks()
    {
        var dir = CreateSkillDir("no-link-skill", """
            ---
            name: no-link-skill
            description: Skill with no links.
            ---
            Body
            """);

        var refs = SkillParser.ResolveFileReferences("Just plain text, no links.", dir);

        Assert.Empty(refs);
    }

    // --- ReadResource tests ---

    [Fact]
    public void ReadResource_ReturnsFileContent()
    {
        var dir = CreateSkillDir("read-resource-skill", """
            ---
            name: read-resource-skill
            description: Skill for reading resources.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "extract.py"), "print('hello world')");

        var content = SkillParser.ReadResource(dir, "scripts/extract.py");

        Assert.Equal("print('hello world')", content);
    }

    [Fact]
    public void ReadResource_ThrowsOnMissingFile()
    {
        var dir = CreateSkillDir("missing-resource-skill", """
            ---
            name: missing-resource-skill
            description: Skill with missing resource.
            ---
            Body
            """);

        var ex = Assert.Throws<SkillParseException>(
            () => SkillParser.ReadResource(dir, "scripts/nonexistent.py"));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void ReadResource_ThrowsOnPathTraversal_DotDot()
    {
        var dir = CreateSkillDir("traversal-skill", """
            ---
            name: traversal-skill
            description: Skill for path traversal test.
            ---
            Body
            """);

        var ex = Assert.Throws<SkillParseException>(
            () => SkillParser.ReadResource(dir, "../../../etc/passwd"));
        Assert.Contains("escapes", ex.Message);
    }

    [Fact]
    public void ReadResource_ThrowsOnPathTraversal_EncodedDots()
    {
        var dir = CreateSkillDir("traversal-encoded-skill", """
            ---
            name: traversal-encoded-skill
            description: Skill for encoded traversal test.
            ---
            Body
            """);

        var ex = Assert.Throws<SkillParseException>(
            () => SkillParser.ReadResource(dir, "scripts/../../etc/passwd"));
        Assert.Contains("escapes", ex.Message);
    }

    [Fact]
    public void ReadResource_ReadsReferenceFile()
    {
        var dir = CreateSkillDir("read-ref-skill", """
            ---
            name: read-ref-skill
            description: Read a reference file.
            ---
            Body
            """);
        var refsDir = Path.Combine(dir, "references");
        Directory.CreateDirectory(refsDir);
        File.WriteAllText(Path.Combine(refsDir, "REFERENCE.md"), "# API Reference\nDetails here.");

        var content = SkillParser.ReadResource(dir, "references/REFERENCE.md");

        Assert.Equal("# API Reference\nDetails here.", content);
    }
}
