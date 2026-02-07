namespace elbruno.Extensions.AI.Skills.Core.Tests;

public class SkillValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public SkillValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-val-test-{Guid.NewGuid():N}");
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
    public void Validate_ValidSkill_ReturnsNoErrors()
    {
        var dir = CreateSkillDir("valid-skill", """
            ---
            name: valid-skill
            description: A valid skill for testing.
            ---
            Instructions here.
            """);

        var errors = SkillValidator.Validate(dir);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NonexistentPath_ReturnsError()
    {
        var errors = SkillValidator.Validate(Path.Combine(_tempDir, "nope"));
        Assert.Single(errors);
        Assert.Contains("does not exist", errors[0]);
    }

    [Fact]
    public void Validate_MissingSkillMd_ReturnsError()
    {
        var dir = Path.Combine(_tempDir, "empty-dir");
        Directory.CreateDirectory(dir);
        var errors = SkillValidator.Validate(dir);
        Assert.Single(errors);
        Assert.Contains("Missing required file", errors[0]);
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsError()
    {
        var longName = new string('a', 65);
        var dir = CreateSkillDir(longName, $"---\nname: {longName}\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("exceeds 64 character limit"));
    }

    [Fact]
    public void Validate_NameUppercase_ReturnsError()
    {
        var dir = CreateSkillDir("Upper", "---\nname: Upper\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("must be lowercase"));
    }

    [Fact]
    public void Validate_NameStartsWithHyphen_ReturnsError()
    {
        var dir = CreateSkillDir("-bad", "---\nname: -bad\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("cannot start or end with a hyphen"));
    }

    [Fact]
    public void Validate_NameEndsWithHyphen_ReturnsError()
    {
        var dir = CreateSkillDir("bad-", "---\nname: bad-\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("cannot start or end with a hyphen"));
    }

    [Fact]
    public void Validate_NameConsecutiveHyphens_ReturnsError()
    {
        var dir = CreateSkillDir("bad--name", "---\nname: bad--name\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("consecutive hyphens"));
    }

    [Fact]
    public void Validate_NameInvalidChars_ReturnsError()
    {
        var dir = CreateSkillDir("bad_name", "---\nname: bad_name\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("invalid characters"));
    }

    [Fact]
    public void Validate_DirectoryNameMismatch_ReturnsError()
    {
        var dir = CreateSkillDir("wrong-dir", "---\nname: correct-name\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("must match skill name"));
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsError()
    {
        var longDesc = new string('x', 1025);
        var dir = CreateSkillDir("long-desc", $"---\nname: long-desc\ndescription: {longDesc}\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("exceeds 1024 character limit"));
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var dir = CreateSkillDir("no-name-v", "---\ndescription: test\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("Missing required field") && e.Contains("name"));
    }

    [Fact]
    public void Validate_MissingDescription_ReturnsError()
    {
        var dir = CreateSkillDir("no-desc-v", "---\nname: no-desc-v\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("Missing required field") && e.Contains("description"));
    }

    [Fact]
    public void Validate_UnexpectedFields_ReturnsError()
    {
        var dir = CreateSkillDir("extra-field", "---\nname: extra-field\ndescription: test\ncustom: value\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("Unexpected fields"));
    }

    [Fact]
    public void Validate_CompatibilityTooLong_ReturnsError()
    {
        var longCompat = new string('y', 501);
        var dir = CreateSkillDir("long-compat", $"---\nname: long-compat\ndescription: test\ncompatibility: {longCompat}\n---\nBody");
        var errors = SkillValidator.Validate(dir);
        Assert.Contains(errors, e => e.Contains("Compatibility exceeds 500 character limit"));
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsNoErrors()
    {
        var dir = CreateSkillDir("full-valid", """
            ---
            name: full-valid
            description: A skill with all fields.
            license: Apache-2.0
            compatibility: Requires git
            allowed-tools: Bash(git:*) Read
            metadata:
              author: test
              version: "1.0"
            ---
            Body content
            """);

        var errors = SkillValidator.Validate(dir);
        Assert.Empty(errors);
    }

    // --- Phase 3: Sub-directory and file reference validation ---

    [Fact]
    public void Validate_EmptyScriptsDir_ReturnsWarning()
    {
        var dir = CreateSkillDir("empty-scripts", """
            ---
            name: empty-scripts
            description: Skill with empty scripts dir.
            ---
            Body
            """);
        Directory.CreateDirectory(Path.Combine(dir, "scripts"));

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.StartsWith(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("scripts/", results[0]);
        Assert.Contains("no files", results[0]);
    }

    [Fact]
    public void Validate_EmptyReferencesDir_ReturnsWarning()
    {
        var dir = CreateSkillDir("empty-refs", """
            ---
            name: empty-refs
            description: Skill with empty references dir.
            ---
            Body
            """);
        Directory.CreateDirectory(Path.Combine(dir, "references"));

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.StartsWith(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("references/", results[0]);
    }

    [Fact]
    public void Validate_EmptyAssetsDir_ReturnsWarning()
    {
        var dir = CreateSkillDir("empty-assets", """
            ---
            name: empty-assets
            description: Skill with empty assets dir.
            ---
            Body
            """);
        Directory.CreateDirectory(Path.Combine(dir, "assets"));

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.StartsWith(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("assets/", results[0]);
    }

    [Fact]
    public void Validate_NonEmptySubDir_NoWarning()
    {
        var dir = CreateSkillDir("full-subdir", """
            ---
            name: full-subdir
            description: Skill with populated scripts dir.
            ---
            Body
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hi");

        var results = SkillValidator.Validate(dir);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_BrokenFileReference_ReturnsWarning()
    {
        var dir = CreateSkillDir("broken-ref", """
            ---
            name: broken-ref
            description: Skill with broken file reference.
            ---
            See [the script](scripts/missing.py) for details.
            """);

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.StartsWith(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("scripts/missing.py", results[0]);
        Assert.Contains("does not exist", results[0]);
    }

    [Fact]
    public void Validate_ValidFileReference_NoWarning()
    {
        var dir = CreateSkillDir("valid-ref", """
            ---
            name: valid-ref
            description: Skill with valid file reference.
            ---
            See [the script](scripts/run.sh) for details.
            """);
        var scriptsDir = Path.Combine(dir, "scripts");
        Directory.CreateDirectory(scriptsDir);
        File.WriteAllText(Path.Combine(scriptsDir, "run.sh"), "echo hi");

        var results = SkillValidator.Validate(dir);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_PathTraversalInReference_ReturnsError()
    {
        var dir = CreateSkillDir("traversal-ref", """
            ---
            name: traversal-ref
            description: Skill with path traversal reference.
            ---
            See [escape](scripts/../../etc/passwd) for details.
            """);

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.DoesNotContain(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("escapes", results[0]);
    }

    [Fact]
    public void Validate_MultipleEmptyDirs_ReturnsMultipleWarnings()
    {
        var dir = CreateSkillDir("multi-empty", """
            ---
            name: multi-empty
            description: Skill with multiple empty dirs.
            ---
            Body
            """);
        Directory.CreateDirectory(Path.Combine(dir, "scripts"));
        Directory.CreateDirectory(Path.Combine(dir, "references"));
        Directory.CreateDirectory(Path.Combine(dir, "assets"));

        var results = SkillValidator.Validate(dir);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.StartsWith(SkillValidator.WarningPrefix, r));
    }

    [Fact]
    public void Validate_InlineCodeBrokenRef_ReturnsWarning()
    {
        var dir = CreateSkillDir("inline-broken", """
            ---
            name: inline-broken
            description: Skill with broken inline code ref.
            ---
            Use `references/REFERENCE.md` for more info.
            """);

        var results = SkillValidator.Validate(dir);

        Assert.Single(results);
        Assert.StartsWith(SkillValidator.WarningPrefix, results[0]);
        Assert.Contains("references/REFERENCE.md", results[0]);
    }
}
