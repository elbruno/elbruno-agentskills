# elbruno.Extensions.AI.Skills

[![NuGet](https://img.shields.io/nuget/v/elbruno.Extensions.AI.Skills.svg)](https://www.nuget.org/packages/elbruno.Extensions.AI.Skills)
[![NuGet Downloads](https://img.shields.io/nuget/dt/elbruno.Extensions.AI.Skills.svg)](https://www.nuget.org/packages/elbruno.Extensions.AI.Skills)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/elbruno/elbruno-agentskills/blob/master/LICENSE)

A .NET 10 library implementing the [Agent Skills specification](https://agentskills.io/specification) — an open format by Anthropic for giving AI agents new capabilities and expertise.

Built with Microsoft.Extensions.AI, the C# MCP SDK, and modern .NET patterns.

**GitHub**: https://github.com/elbruno/elbruno-agentskills

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| **elbruno.Extensions.AI.Skills.Core** | [![NuGet](https://img.shields.io/nuget/v/elbruno.Extensions.AI.Skills.Core.svg)](https://www.nuget.org/packages/elbruno.Extensions.AI.Skills.Core) | Models, SKILL.md parser, validator, and prompt XML generator. Minimal dependencies. |
| **elbruno.Extensions.AI.Skills** | [![NuGet](https://img.shields.io/nuget/v/elbruno.Extensions.AI.Skills.svg)](https://www.nuget.org/packages/elbruno.Extensions.AI.Skills) | DI extensions, `IChatClient` middleware, `ISkillProvider`, and MCP bridge. |
| **elbruno.Extensions.AI.Skills.Cli** | [![NuGet](https://img.shields.io/nuget/v/elbruno.Extensions.AI.Skills.Cli.svg)](https://www.nuget.org/packages/elbruno.Extensions.AI.Skills.Cli) | .NET global tool for validating skills and generating prompt XML. |

## Quick Start

### Install

```bash
dotnet add package elbruno.Extensions.AI.Skills
```

### Point to a skills folder and use with an agent

This is all you need — point the library at a folder containing skills, and they are automatically discovered and injected into your agent's system prompt:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using elbruno.Extensions.AI.Skills;

var services = new ServiceCollection();

// Register your LLM client (Ollama, OpenAI, Anthropic, etc.)
services.AddSingleton<IChatClient>(new OllamaApiClient("http://localhost:11434", "llama3.2"));

// Point to a folder with skills — they are auto-discovered and injected into prompts
services.AddAgentSkills(o => o.AutoDiscover = true)
    .WithSkillDirectories("./skills")
    .WithChatClient();

var sp = services.BuildServiceProvider();
var chatClient = sp.GetRequiredService<IChatClient>();

// That's it! Skills are automatically injected as <available_skills> XML in system prompts
var response = await chatClient.GetResponseAsync([
    new(ChatRole.System, "You are a helpful coding assistant."),
    new(ChatRole.User, "Review this code for security issues: var sql = \"SELECT * FROM Users WHERE Name='\" + name + \"'\";")
]);

Console.WriteLine(response.Text);
```

The `./skills` folder contains skill subdirectories, each with a `SKILL.md` file. The `SkillsChatClient` middleware automatically injects all discovered skills into every request's system prompt — no manual wiring needed.

### Core API (parse, validate, generate)

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

### DI + IChatClient Integration

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

### Wrap IChatClient with Skills

```csharp
// SkillsChatClient injects <available_skills> XML into system prompts
IChatClient innerClient = /* your chat client */;
var skillsClient = new SkillsChatClient(innerClient, provider);

// Skills are automatically injected when calling the chat client
var response = await skillsClient.GetResponseAsync(messages);
```

### MCP Bridge

```csharp
// Expose skills as MCP resources
var bridge = new SkillsMcpBridge(provider, logger);
var handlers = new McpServerHandlers();
bridge.ConfigureHandlers(handlers);

// Skills are now accessible via MCP as skill://{name}/SKILL.md resources
```

### CLI Tool

```bash
# Install globally
dotnet tool install -g elbruno.Extensions.AI.Skills.Cli

# Validate a skill
skills validate ./my-skill

# Read properties as JSON
skills read-properties ./my-skill

# Generate <available_skills> XML
skills to-prompt ./skill-a ./skill-b
```

## Agent Skills Format

A skill is a directory containing a `SKILL.md` file:

```
my-skill/
├── SKILL.md          # Required: YAML frontmatter + Markdown instructions
├── scripts/          # Optional: executable code
├── references/       # Optional: additional docs
└── assets/           # Optional: templates, resources
```

### SKILL.md Example

```markdown
---
name: code-review
description: Reviews code for bugs, security issues, and best practices.
license: Apache-2.0
compatibility: Requires git
---

# Code Review

## When to use
Use when the user asks you to review code or audit security.

## Steps
1. Read the code carefully
2. Check for common bug patterns
3. Look for security vulnerabilities
```

## Architecture

The library follows the same patterns as the [C# MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk):

- **Multi-package layout**: Core (minimal deps) + Main (full integration)
- **DI-first**: `AddAgentSkills().WithSkillDirectories()` fluent API
- **IChatClient middleware**: `SkillsChatClient` as `DelegatingChatClient`
- **Progressive disclosure**: Only metadata at startup, full content on activation

## Dependencies

- **.NET 10**
- **YamlDotNet** — YAML frontmatter parsing
- **Microsoft.Extensions.AI** — IChatClient integration
- **ModelContextProtocol** — MCP bridge

## Roadmap

This is **v0.1** — the foundational layer for the [Agent Skills specification](https://agentskills.io/specification). The following spec features are **not yet supported** and planned for future releases:

| Feature | Spec Section | Status |
|---------|-------------|--------|
| **`scripts/` directory** | Executable code (Python, Bash, JS) that agents can run | Planned |
| **`references/` directory** | Additional docs loaded on demand (`REFERENCE.md`, domain files) | Planned |
| **`assets/` directory** | Static resources — templates, images, data files, schemas | Planned |
| **File reference resolution** | Resolving relative paths from `SKILL.md` to bundled files | Planned |
| **Progressive disclosure (Level 3)** | On-demand loading of resources from `scripts/`, `references/`, `assets/` | Planned |

Contributions and feedback are welcome!

## License

MIT
