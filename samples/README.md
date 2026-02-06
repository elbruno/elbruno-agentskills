# Samples

## Shared Resources

All samples share these directories at the `samples/` level:

| Directory | Description |
|-----------|-------------|
| `skills/code-review/` | Reviews C# code for bugs, security issues, and best practices |
| `sample-code/` | C# files with varying code quality for testing skills |

### Sample Code Files

| File | Quality | Issues |
|------|---------|--------|
| `sample-code/GoodCode.cs` | Good | Clean code following best practices |
| `sample-code/BadCode.cs` | Bad | Blocking async, undisposed resources, off-by-one error |
| `sample-code/VeryBadCode.cs` | Very bad | SQL injection, hardcoded secrets, path traversal |

## BasicSkillUsage

Demonstrates the Core API: loading a skill from disk, validating it, generating prompt XML, and using the skill's instructions to analyze the shared sample C# files.

### What it shows

1. **Validate** a skill directory with `SkillValidator.Validate()`
2. **Load** a full skill (properties + body) with `SkillParser.ReadSkill()`
3. **Generate** `<available_skills>` XML with `SkillPromptGenerator.ToPromptXml()`
4. **Build a review prompt** combining the skill's instructions with source code

### Run

```bash
dotnet run --project samples/BasicSkillUsage
```

## AgentWithSkills

**Complete AI agent example** using [OllamaSharp](https://github.com/awaescher/OllamaSharp) with a local Ollama model, demonstrating how skills are automatically injected into LLM conversations via Microsoft.Extensions.AI.

### Prerequisites

- [Ollama](https://ollama.com) running locally (default: `http://localhost:11434`)
- A model pulled, e.g.: `ollama pull llama3.2`

### What it shows

1. **Dependency Injection** setup with `IServiceCollection`
2. **OllamaSharp as `IChatClient`** — real LLM calls via Ollama's local API
3. **Register skills** with `AddAgentSkills().WithSkillDirectories().WithChatClient()`
4. **Automatic skill injection** — `SkillsChatClient` middleware adds `<available_skills>` XML to system prompts
5. **Reviews shared sample code** — Sends each file from `sample-code/` to Ollama for review using the injected skill

### Configuration

Edit the top of `Program.cs` to change the endpoint or model:

```csharp
var ollamaEndpoint = "http://localhost:11434";
var ollamaModel = "llama3.2";
```

### Run

```bash
dotnet run --project samples/AgentWithSkills
```
