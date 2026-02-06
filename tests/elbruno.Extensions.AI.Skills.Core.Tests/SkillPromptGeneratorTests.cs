namespace elbruno.Extensions.AI.Skills.Core.Tests;

public class SkillPromptGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public SkillPromptGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-prompt-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateSkillDir(string name, string description)
    {
        var skillDir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"),
            $"---\nname: {name}\ndescription: {description}\n---\nBody");
        return skillDir;
    }

    [Fact]
    public void ToPromptXml_EmptyList_ReturnsEmptyBlock()
    {
        var result = SkillPromptGenerator.ToPromptXml([]);
        Assert.Equal("<available_skills>\n</available_skills>", result);
    }

    [Fact]
    public void ToPromptXml_SingleSkill_GeneratesCorrectXml()
    {
        var dir = CreateSkillDir("pdf-reader", "Read and extract text from PDF files");
        var result = SkillPromptGenerator.ToPromptXml([dir]);

        Assert.Contains("<available_skills>", result);
        Assert.Contains("</available_skills>", result);
        Assert.Contains("<name>\npdf-reader\n</name>", result);
        Assert.Contains("<description>\nRead and extract text from PDF files\n</description>", result);
        Assert.Contains("<location>", result);
        Assert.Contains("SKILL.md", result);
    }

    [Fact]
    public void ToPromptXml_MultipleSkills_GeneratesAllEntries()
    {
        var dir1 = CreateSkillDir("skill-a", "First skill");
        var dir2 = CreateSkillDir("skill-b", "Second skill");

        var result = SkillPromptGenerator.ToPromptXml([dir1, dir2]);

        Assert.Contains("skill-a", result);
        Assert.Contains("skill-b", result);
        Assert.Equal(2, result.Split("<skill>").Length - 1);
    }

    [Fact]
    public void ToPromptXml_EscapesHtml()
    {
        var dir = CreateSkillDir("html-skill", "Uses <tags> & stuff");
        var result = SkillPromptGenerator.ToPromptXml([dir]);

        Assert.Contains("&lt;tags&gt;", result);
        Assert.Contains("&amp;", result);
    }
}
