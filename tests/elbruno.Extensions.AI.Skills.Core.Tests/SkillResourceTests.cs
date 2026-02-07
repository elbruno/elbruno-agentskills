namespace elbruno.Extensions.AI.Skills.Core.Tests;

public class SkillResourceTests
{
    [Fact]
    public void SkillResourceType_HasExpectedValues()
    {
        Assert.Equal(0, (int)SkillResourceType.Script);
        Assert.Equal(1, (int)SkillResourceType.Reference);
        Assert.Equal(2, (int)SkillResourceType.Asset);
    }

    [Fact]
    public void SkillResource_StoresAllProperties()
    {
        var resource = new SkillResource(
            SkillResourceType.Script,
            "scripts/extract.py",
            "/full/path/scripts/extract.py",
            "extract.py",
            ".py");

        Assert.Equal(SkillResourceType.Script, resource.Type);
        Assert.Equal("scripts/extract.py", resource.RelativePath);
        Assert.Equal("/full/path/scripts/extract.py", resource.AbsolutePath);
        Assert.Equal("extract.py", resource.FileName);
        Assert.Equal(".py", resource.Extension);
    }

    [Fact]
    public void SkillResource_SupportsValueEquality()
    {
        var a = new SkillResource(SkillResourceType.Reference, "references/REFERENCE.md", "/abs/references/REFERENCE.md", "REFERENCE.md", ".md");
        var b = new SkillResource(SkillResourceType.Reference, "references/REFERENCE.md", "/abs/references/REFERENCE.md", "REFERENCE.md", ".md");

        Assert.Equal(a, b);
    }

    [Fact]
    public void SkillResource_DifferentType_NotEqual()
    {
        var a = new SkillResource(SkillResourceType.Script, "scripts/run.sh", "/abs/scripts/run.sh", "run.sh", ".sh");
        var b = new SkillResource(SkillResourceType.Asset, "scripts/run.sh", "/abs/scripts/run.sh", "run.sh", ".sh");

        Assert.NotEqual(a, b);
    }
}
