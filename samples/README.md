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
