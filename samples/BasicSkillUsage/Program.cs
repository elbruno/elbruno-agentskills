using elbruno.Extensions.AI.Skills.Core;

// === Sample: Load a Skill and Analyze C# Files ===

Console.WriteLine("=== Agent Skills - Code Review Analysis ===\n");

// 1. Resolve the shared skills directory (samples/skills/code-review)
var skillsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "skills"));
var codeReviewDir = Path.Combine(skillsDir, "code-review");

// 2. Validate the skill
var errors = SkillValidator.Validate(codeReviewDir);
if (errors.Count > 0)
{
    Console.WriteLine($"Skill validation failed:");
    foreach (var error in errors)
        Console.WriteLine($"  - {error}");
    return;
}

// 3. Load the full skill (properties + instructions body)
var skill = SkillParser.ReadSkill(codeReviewDir);
Console.WriteLine($"Loaded skill: {skill.Properties.Name}");
Console.WriteLine($"  Description: {skill.Properties.Description}");
Console.WriteLine($"  License: {skill.Properties.License}");
Console.WriteLine();

// 4. Generate the <available_skills> XML that would be injected into an agent prompt
var promptXml = SkillPromptGenerator.ToPromptXml([codeReviewDir]);
Console.WriteLine("Prompt XML for the agent:");
Console.WriteLine(promptXml);
Console.WriteLine();

// 5. Analyze sample C# files using the skill's review checklist
var sampleCodeDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "sample-code"));
var sampleFiles = Directory.GetFiles(sampleCodeDir, "*.cs");

Console.WriteLine($"=== Analyzing {sampleFiles.Length} files with '{skill.Properties.Name}' skill ===\n");
Console.WriteLine($"Review checklist from skill instructions ({skill.Body.Length} chars):");
Console.WriteLine(new string('-', 60));

foreach (var file in sampleFiles.OrderBy(f => f))
{
    var fileName = Path.GetFileName(file);
    var sourceCode = File.ReadAllText(file);

    Console.WriteLine($"\nFile: {fileName} ({sourceCode.Split('\n').Length} lines)");
    Console.WriteLine(new string('-', 40));

    // Demonstrate how the skill body provides review criteria
    // In a real scenario, this would be sent to an LLM along with the source code
    var reviewPrompt = $"""
        You are a code reviewer. Use the following skill instructions:

        {skill.Body}

        Review this C# file:

        ```csharp
        {sourceCode}
        ```
        """;

    Console.WriteLine($"  Generated review prompt: {reviewPrompt.Length} chars");
    Console.WriteLine($"  Source preview: {sourceCode[..Math.Min(80, sourceCode.Length)].Trim()}...");

    // Simple static analysis to demonstrate the skill's checklist in action
    var issues = new List<string>();

    if (sourceCode.Contains(".Result"))
        issues.Add("[Warning] Blocking async call with .Result detected");
    if (sourceCode.Contains("new HttpClient()"))
        issues.Add("[Warning] HttpClient created directly instead of being injected");
    if (sourceCode.Contains("catch (Exception)") && !sourceCode.Contains("// rethrow"))
        issues.Add("[Warning] Swallowed exception detected");
    if (sourceCode.Contains("Password=") || sourceCode.Contains("password =", StringComparison.OrdinalIgnoreCase))
        issues.Add("[Critical] Hardcoded credentials detected");
    if (sourceCode.Contains("+ userName") || sourceCode.Contains("+ \"'\""))
        issues.Add("[Critical] Possible SQL injection");
    if (sourceCode.Contains("Thread.Sleep"))
        issues.Add("[Warning] Thread.Sleep used in production code");
    if (sourceCode.Contains("<= items.Count"))
        issues.Add("[Bug] Off-by-one error in loop boundary");

    if (issues.Count == 0)
        Console.WriteLine("  Result: No issues found");
    else
        foreach (var issue in issues)
            Console.WriteLine($"  {issue}");
}

Console.WriteLine($"\nDone! Skill '{skill.Properties.Name}' loaded from: {skill.Location}");
