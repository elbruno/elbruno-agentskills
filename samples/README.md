# Samples

## Shared Skills

All samples share skills from the `skills/` directory:

| Skill | Description |
|-------|-------------|
| `skills/code-review/` | Reviews C# code for bugs, security issues, and best practices |

## BasicSkillUsage

Demonstrates the Core API: loading a skill from disk, validating it, generating prompt XML, and using the skill's instructions to analyze C# source files.

The sample includes three C# files with varying code quality:

| File | Quality | Issues |
|------|---------|--------|
| `sample-code/GoodCode.cs` | Good | Clean code following best practices |
| `sample-code/BadCode.cs` | Bad | Blocking async, undisposed resources, off-by-one error |
| `sample-code/VeryBadCode.cs` | Very bad | SQL injection, hardcoded secrets, path traversal |

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

**Complete AI agent example** showing how to create an agent that uses skills in its reasoning process with Microsoft.Extensions.AI.

### What it shows

1. **Dependency Injection** setup with `IServiceCollection`
2. **Register skills** with `AddAgentSkills().WithSkillDirectories().WithChatClient()`
3. **Automatic skill injection** - `SkillsChatClient` middleware adds `<available_skills>` XML to system prompts
4. **Multi-turn conversations** - Agent maintains context across multiple user messages
5. **Mock LLM client** - Demonstrates the IChatClient pattern (replace with real LLM provider)

### Setup Instructions

Due to a technical limitation, please manually create the directory and files:

**1. Create the directory:**
```bash
mkdir samples\AgentWithSkills
```

**2. Create `samples\AgentWithSkills\AgentWithSkills.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>AgentWithSkills</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\elbruno.Extensions.AI.Skills\elbruno.Extensions.AI.Skills.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" />
  </ItemGroup>

</Project>
```

**3. Create `samples\AgentWithSkills\Program.cs`** - see full source below.

### Run

```bash
dotnet run --project samples/AgentWithSkills
```
