using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using elbruno.Extensions.AI.Skills.Core;

var rootCommand = new RootCommand("CLI tool for Agent Skills - validate, read properties, and generate prompt XML.");

// validate command
var validatePathArg = new Argument<DirectoryInfo>("path", "Path to the skill directory to validate.");
var validateCommand = new Command("validate", "Validate a skill directory.");
validateCommand.AddArgument(validatePathArg);
validateCommand.SetHandler((DirectoryInfo path) =>
{
    var skillPath = path.FullName;
    if (File.Exists(skillPath) && Path.GetFileName(skillPath).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase))
        skillPath = Path.GetDirectoryName(skillPath)!;

    var errors = SkillValidator.Validate(skillPath);
    if (errors.Count > 0)
    {
        Console.Error.WriteLine($"Validation failed for {skillPath}:");
        foreach (var error in errors)
            Console.Error.WriteLine($"  - {error}");
        Environment.ExitCode = 1;
    }
    else
    {
        Console.WriteLine($"Valid skill: {skillPath}");
    }
}, validatePathArg);

// read-properties command
var readPropsPathArg = new Argument<DirectoryInfo>("path", "Path to the skill directory.");
var readPropsCommand = new Command("read-properties", "Read and print skill properties as JSON.");
readPropsCommand.AddArgument(readPropsPathArg);
readPropsCommand.SetHandler((DirectoryInfo path) =>
{
    try
    {
        var skillPath = path.FullName;
        if (File.Exists(skillPath) && Path.GetFileName(skillPath).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase))
            skillPath = Path.GetDirectoryName(skillPath)!;

        var props = SkillParser.ReadProperties(skillPath);
        Console.WriteLine(JsonSerializer.Serialize(props.ToDictionary(), new JsonSerializerOptions { WriteIndented = true }));
    }
    catch (SkillException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, readPropsPathArg);

// to-prompt command
var toPromptPathsArg = new Argument<DirectoryInfo[]>("paths", "Paths to skill directories.") { Arity = ArgumentArity.OneOrMore };
var toPromptCommand = new Command("to-prompt", "Generate <available_skills> XML for agent prompts.");
toPromptCommand.AddArgument(toPromptPathsArg);
toPromptCommand.SetHandler((DirectoryInfo[] paths) =>
{
    try
    {
        var resolvedPaths = paths.Select(p =>
        {
            var pth = p.FullName;
            if (File.Exists(pth) && Path.GetFileName(pth).Equals("SKILL.md", StringComparison.OrdinalIgnoreCase))
                pth = Path.GetDirectoryName(pth)!;
            return pth;
        }).ToList();

        var output = SkillPromptGenerator.ToPromptXml(resolvedPaths);
        Console.WriteLine(output);
    }
    catch (SkillException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = 1;
    }
}, toPromptPathsArg);

rootCommand.AddCommand(validateCommand);
rootCommand.AddCommand(readPropsCommand);
rootCommand.AddCommand(toPromptCommand);

return await rootCommand.InvokeAsync(args);
