# Copilot Instructions for elbruno.Extensions.AI.Skills

## Project Overview

This is a .NET 10 library implementing the [Agent Skills specification](https://agentskills.io/specification) by Anthropic. It provides parsing, validation, and integration tools for AI agent skills.

## Architecture

**Three-package structure** (following C# MCP SDK patterns):

| Package | Purpose | Dependencies |
|---------|---------|--------------|
| `elbruno.Extensions.AI.Skills.Core` | Models, parser, validator, prompt generator | Minimal (YamlDotNet only) |
| `elbruno.Extensions.AI.Skills` | DI, `IChatClient` middleware, `ISkillProvider`, MCP bridge | Core + Microsoft.Extensions.AI |
| `elbruno.Extensions.AI.Skills.Cli` | Global tool for CLI validation | Core + System.CommandLine |

## Key Patterns

### Static utility classes for Core operations
Core uses static classes (`SkillParser`, `SkillValidator`, `SkillPromptGenerator`) - no instantiation needed:
```csharp
var props = SkillParser.ReadProperties("./skill-dir");
var errors = SkillValidator.Validate("./skill-dir");
```

### Exception hierarchy
Use `SkillParseException` for file/YAML issues, `SkillValidationException` for spec violations. Both inherit from `SkillException`.

### DI-first integration
Register via `services.AddAgentSkills()` with fluent builder pattern:
```csharp
services.AddAgentSkills(o => o.AutoDiscover = true)
    .WithSkillDirectories("./skills")
    .WithChatClient();
```

### IChatClient middleware
`SkillsChatClient` is a `DelegatingChatClient` that injects `<available_skills>` XML into system prompts automatically.

### Progressive disclosure
`ISkillProvider.GetSkillMetadata()` returns lightweight properties; `GetSkill(name)` loads full content on-demand.

## Build & Test Commands

```bash
dotnet build AgentSkills.slnx           # Build all projects
dotnet test                              # Run all tests
dotnet run --project src/elbruno.Extensions.AI.Skills.Cli -- validate ./skill-dir
```

## Testing Conventions

- Tests use xUnit with `IDisposable` for temp directory cleanup
- Use `CreateSkillDir(name, content)` helper pattern for test fixtures
- Mock `ISkillProvider` with NSubstitute for integration layer tests

## SKILL.md Format

Skills are directories containing `SKILL.md` with YAML frontmatter:
```markdown
---
name: lowercase-hyphenated
description: Under 1024 chars
license: MIT
compatibility: Optional requirements
allowed-tools: Bash(git:*) Read
metadata:
  author: org-name
---
# Instructions body
```

**Validation rules:**
- `name` must be lowercase, hyphenated, max 64 chars, match directory name
- `description` required, max 1024 chars
- Only allowed frontmatter fields: `name`, `description`, `license`, `compatibility`, `allowed-tools`, `metadata`

## Central Package Management

Versions defined in `Directory.Packages.props` - never add versions in individual `.csproj` files.

## Repository Structure

**Root directory** should only contain:
- `README.md`, `LICENSE` - project info
- `*.slnx`, `*.props`, `global.json` - build configuration
- `src/`, `tests/`, `samples/` - code folders
- `docs/` - all documentation

**Documentation convention**: Place all documentation files in the `docs/` folder at the repository root. Do not create markdown files or documentation at the root level (except README.md).
