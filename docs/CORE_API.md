# Core API

The `elbruno.Extensions.AI.Skills.Core` package provides static utility classes for parsing, validating, and generating prompt XML from skill directories.

## Install

```bash
dotnet add package elbruno.Extensions.AI.Skills.Core
```

## Parse, Validate, Generate

```csharp
using elbruno.Extensions.AI.Skills.Core;

// Validate a skill directory
var errors = SkillValidator.Validate("./my-skill");

// Read skill properties from SKILL.md frontmatter
var props = SkillParser.ReadProperties("./my-skill");
Console.WriteLine($"{props.Name}: {props.Description}");

// Generate <available_skills> XML for agent prompts
var xml = SkillPromptGenerator.ToPromptXml(["./skill-a", "./skill-b"]);
```

## DI + IChatClient Integration

```csharp
using elbruno.Extensions.AI.Skills;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register skills with DI
services.AddAgentSkills(options =>
{
    options.SkillDirectories.Add("./skills");
    options.AutoDiscover = true;
});

// Use ISkillProvider to discover and activate skills
var provider = sp.GetRequiredService<ISkillProvider>();
foreach (var skill in provider.GetSkillMetadata())
    Console.WriteLine($"  {skill.Name}: {skill.Description}");

// Activate a skill (load full SKILL.md content)
var fullSkill = provider.GetSkill("code-review");
```

## Wrap IChatClient with Skills

```csharp
// SkillsChatClient injects <available_skills> XML into system prompts
IChatClient innerClient = /* your chat client */;
var skillsClient = new SkillsChatClient(innerClient, provider);

// Skills are automatically injected when calling the chat client
var response = await skillsClient.GetResponseAsync(messages);
```

## MCP Bridge

```csharp
// Expose skills as MCP resources
var bridge = new SkillsMcpBridge(provider, logger);
var handlers = new McpServerHandlers();
bridge.ConfigureHandlers(handlers);

// Skills are now accessible via MCP as skill://{name}/SKILL.md resources
```
