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

```bash
dotnet add package elbruno.Extensions.AI.Skills
```

Point the library at a folder containing skills, and they are automatically discovered and injected into your agent's system prompt:

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

## Documentation

| Document | Description |
|----------|-------------|
| [Core API](docs/CORE_API.md) | Parser, validator, prompt generator, DI integration, MCP bridge |
| [CLI Tool](docs/CLI.md) | Command-line tool for validating skills and generating XML |
| [Skill Format](docs/SKILL_FORMAT.md) | SKILL.md format, frontmatter fields, validation rules |
| [Architecture](docs/ARCHITECTURE.md) | Package structure, design patterns, dependencies |
| [Agent Sample](docs/AGENT_SAMPLE_IMPLEMENTATION.md) | Full agent sample with mock chat client |

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
