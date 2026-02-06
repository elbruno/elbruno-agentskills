# Architecture

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

## Package Structure

| Package | Purpose | Dependencies |
|---------|---------|--------------|
| `elbruno.Extensions.AI.Skills.Core` | Models, parser, validator, prompt generator | Minimal (YamlDotNet only) |
| `elbruno.Extensions.AI.Skills` | DI, `IChatClient` middleware, `ISkillProvider`, MCP bridge | Core + Microsoft.Extensions.AI |
| `elbruno.Extensions.AI.Skills.Cli` | Global tool for CLI validation | Core + System.CommandLine |

## Key Patterns

### Static utility classes for Core operations

Core uses static classes (`SkillParser`, `SkillValidator`, `SkillPromptGenerator`) — no instantiation needed.

### Exception hierarchy

`SkillParseException` for file/YAML issues, `SkillValidationException` for spec violations. Both inherit from `SkillException`.

### DI-first integration

Register via `services.AddAgentSkills()` with fluent builder pattern.

### IChatClient middleware

`SkillsChatClient` is a `DelegatingChatClient` that injects `<available_skills>` XML into system prompts automatically.

### Progressive disclosure

`ISkillProvider.GetSkillMetadata()` returns lightweight properties; `GetSkill(name)` loads full content on-demand.
