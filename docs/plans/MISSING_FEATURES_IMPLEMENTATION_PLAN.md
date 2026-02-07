# Implementation Plan: Remaining Agent Skills Spec Features

**Date**: 2026-02-07  
**Spec**: https://agentskills.io/specification  
**Scope**: All 5 roadmap features + full script execution pipeline + MCP bridge exposure

---

## Overview

This plan covers the 5 features listed as "Planned" in the README roadmap:

| Feature | Spec Section |
|---------|-------------|
| `scripts/` directory | Executable code (Python, Bash, JS) that agents can run |
| `references/` directory | Additional docs loaded on demand (`REFERENCE.md`, domain files) |
| `assets/` directory | Static resources — templates, images, data files, schemas |
| File reference resolution | Resolving relative paths from `SKILL.md` to bundled files |
| Progressive disclosure (Level 3) | On-demand loading of resources from `scripts/`, `references/`, `assets/` |

Additionally: full script execution pipeline with sandboxing/confirmation, and MCP bridge exposure for all bundled resources.

---

## Phase 1: Core Models & Constants

### Step 1 — Add directory name constants

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillConstants.cs`

Add:

```csharp
public const string ScriptsDirectory = "scripts";
public const string ReferencesDirectory = "references";
public const string AssetsDirectory = "assets";
```

### Step 2 — Create `SkillResource` model

**New file**: `src/elbruno.Extensions.AI.Skills.Core/SkillResource.cs`

```csharp
public enum SkillResourceType { Script, Reference, Asset }

public record SkillResource(
    SkillResourceType Type,
    string RelativePath,   // from skill root, e.g. "scripts/extract.py"
    string AbsolutePath,
    string FileName,
    string Extension);
```

This is Level 3 metadata — lightweight file descriptors with content loaded on demand.

### Step 3 — Extend `SkillInfo`

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillInfo.cs`

Add three new properties:

```csharp
public record SkillInfo(
    SkillProperties Properties,
    string Body,
    string Location,
    IReadOnlyList<SkillResource> Scripts,
    IReadOnlyList<SkillResource> References,
    IReadOnlyList<SkillResource> Assets);
```

Keep backward-compatible with default empty lists where possible.

---

## Phase 2: Parser — Directory Scanning & File Reference Resolution

### Step 4 — Extend `SkillParser.ReadSkill()`

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillParser.cs`

After parsing SKILL.md, scan for `scripts/`, `references/`, `assets/` subdirectories within the skill directory. Enumerate files (non-recursive, one level deep per spec guidance) and build `SkillResource` lists. Populate the new `SkillInfo` properties.

### Step 5 — Add `SkillParser.ResolveFileReferences()`

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillParser.cs`

New static method:

```csharp
public static IReadOnlyList<SkillResource> ResolveFileReferences(string body, string skillDir)
```

- Parses Markdown links and inline code references matching relative paths (e.g., `references/REFERENCE.md`, `scripts/extract.py`)
- Returns `IReadOnlyList<SkillResource>` of referenced files that actually exist on disk
- Enables agents to discover which resources the skill body explicitly refers to

### Step 6 — Add `SkillParser.ReadResource()`

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillParser.cs`

New static method:

```csharp
public static string ReadResource(string skillDir, string relativePath)
```

- Validates the relative path is within the skill directory (prevents path traversal)
- Reads and returns the file content as a string
- Throws `SkillParseException` if the file doesn't exist or path escapes the skill root

---

## Phase 3: Validator Enhancements

### Step 7 — Validate sub-directories and file references

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillValidator.cs`

Extend `Validate()` to optionally validate sub-directories:

- Warn (not error) if `scripts/`, `references/`, `assets/` contain zero files
- Warn if the body references files (via Markdown links) that don't exist in the skill directory
- Validate that file references stay one level deep (no `../../` escapes)

---

## Phase 4: Prompt Generator Update

### Step 8 — Include resource metadata in XML

**File**: `src/elbruno.Extensions.AI.Skills.Core/SkillPromptGenerator.cs`

Extend `ToPromptXml()` to include resource metadata inside each `<skill>` element:

```xml
<skill>
  <name>pdf-processing</name>
  <description>...</description>
  <location>...</location>
  <scripts>
    <file>extract.py</file>
  </scripts>
  <references>
    <file>REFERENCE.md</file>
  </references>
  <assets>
    <file>template.docx</file>
  </assets>
</skill>
```

Only file names — not content. Matches progressive disclosure principle.

---

## Phase 5: ISkillProvider — Level 3 API

### Step 9 — Add Level 3 methods to `ISkillProvider`

**File**: `src/elbruno.Extensions.AI.Skills/ISkillProvider.cs`

```csharp
string? ReadResource(string skillName, string relativePath);
IReadOnlyList<SkillResource> GetResources(string skillName);
```

### Step 10 — Implement in `FileSystemSkillProvider`

**File**: `src/elbruno.Extensions.AI.Skills/FileSystemSkillProvider.cs`

- `ReadResource` validates the path, reads the file, returns content
- `GetResources` returns the `SkillResource` lists from cached `SkillInfo`
- Resources are enumerated during `Refresh()` but content loaded lazily

---

## Phase 6: Script Execution Pipeline

### Step 11 — Create `IScriptExecutor` interface

**New file**: `src/elbruno.Extensions.AI.Skills/IScriptExecutor.cs`

```csharp
public interface IScriptExecutor
{
    Task<ScriptResult> ExecuteAsync(
        string skillName,
        SkillResource script,
        ScriptExecutionOptions options,
        CancellationToken ct = default);
}

public record ScriptResult(int ExitCode, string StandardOutput, string StandardError, TimeSpan Duration);

public record ScriptExecutionOptions
{
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string>? EnvironmentVariables { get; init; }
}
```

### Step 12 — Create `ProcessScriptExecutor`

**New file**: `src/elbruno.Extensions.AI.Skills/ProcessScriptExecutor.cs`

- Uses `System.Diagnostics.Process` to run scripts
- Resolves script runner by extension: `.py` → `python`, `.sh` → `bash`/`sh`, `.js` → `node`, `.ps1` → `pwsh`
- Enforces timeout from `ScriptExecutionOptions`
- Captures stdout/stderr

### Step 13 — Create `ScriptExecutionGuard`

**New file**: `src/elbruno.Extensions.AI.Skills/ScriptExecutionGuard.cs`

```csharp
public interface IScriptConfirmationHandler
{
    Task<bool> ConfirmExecutionAsync(string skillName, SkillResource script);
}

public class ScriptExecutionGuard
{
    // Checks SkillsOptions.TrustedSkills — if trusted, auto-approve
    // Checks SkillsOptions.RequireConfirmation — if true and not trusted, invoke handler
}
```

Wires the existing `TrustedSkills` and `RequireConfirmation` options already declared in `SkillsOptions.cs` but currently unused.

### Step 14 — Register in DI

**File**: `src/elbruno.Extensions.AI.Skills/SkillsServiceCollectionExtensions.cs`

Add builder methods:

```csharp
ISkillsBuilder WithScriptExecution();
ISkillsBuilder WithScriptConfirmation<T>() where T : class, IScriptConfirmationHandler;
```

---

## Phase 7: MCP Bridge Extension

### Step 15 — Expose resources via MCP

**File**: `src/elbruno.Extensions.AI.Skills/SkillsMcpBridge.cs`

- New URI patterns: `skill://{name}/scripts/{file}`, `skill://{name}/references/{file}`, `skill://{name}/assets/{file}`
- `GetSkillResources()` returns `Resource` entries for all bundled files (not just SKILL.md)
- `ReadSkillResource()` handles the extended URI patterns using `SkillParser.ReadResource()`
- Optionally expose `ExecuteScript` as an MCP tool (gated behind `TrustedSkills`)

---

## Phase 8: SkillsChatClient Enhancement

### Step 16 — Updated prompt injection

**File**: `src/elbruno.Extensions.AI.Skills/SkillsChatClient.cs`

No additional logic needed — the updated `SkillPromptGenerator.ToPromptXml()` output will automatically include resource metadata in the injected `<available_skills>` XML.

---

## Phase 9: CLI Enhancements

### Step 17 — Add new CLI commands

**File**: `src/elbruno.Extensions.AI.Skills.Cli/Program.cs`

| Command | Description |
|---------|-------------|
| `list-resources <path>` | Lists all files in scripts/, references/, assets/ with types |
| `read-resource <path> <relative-path>` | Outputs a single resource file's content |
| `check-references <path>` | Validates that file references in SKILL.md body resolve to real files |

---

## Phase 10: Tests

### Step 18 — `SkillResource` model tests

New tests for the `SkillResource` record and `SkillResourceType` enum in the Core test project.

### Step 19 — Extend `SkillParserTests`

**File**: `tests/elbruno.Extensions.AI.Skills.Core.Tests/SkillParserTests.cs`

- `ReadSkill` populates scripts/references/assets when directories exist
- `ReadSkill` returns empty lists when directories don't exist
- `ResolveFileReferences` finds valid markdown links
- `ReadResource` returns content, rejects path traversal attempts

### Step 20 — Extend `SkillValidatorTests`

**File**: `tests/elbruno.Extensions.AI.Skills.Core.Tests/SkillValidatorTests.cs`

- Validation warns on empty sub-directories
- Validation warns on broken file references in body

### Step 21 — Extend `SkillPromptGeneratorTests`

**File**: `tests/elbruno.Extensions.AI.Skills.Core.Tests/SkillPromptGeneratorTests.cs`

- XML output includes `<scripts>`, `<references>`, `<assets>` elements

### Step 22 — Extend `FileSystemSkillProviderTests`

**File**: `tests/elbruno.Extensions.AI.Skills.Tests/FileSystemSkillProviderTests.cs`

- `GetResources` returns correct resources
- `ReadResource` loads on demand, rejects invalid paths

### Step 23 — Script execution tests

**New file**: `tests/elbruno.Extensions.AI.Skills.Tests/ScriptExecutionTests.cs`

- Trusted skills execute without confirmation
- Untrusted skills invoke confirmation handler
- Timeout enforcement
- Extension-to-runtime mapping

### Step 24 — MCP bridge tests

Extend existing MCP bridge tests for new resource URI patterns.

---

## Phase 11: Sample Skill Update

### Step 25 — Enhance the sample skill

**Directory**: `samples/skills/code-review/`

- Add `scripts/lint-check.sh` (or `.ps1`) — sample script
- Add `references/REFERENCE.md` — detailed reference doc
- Add `assets/review-template.md` — output template
- Add file references in `SKILL.md` body pointing to these resources

Makes the sample a complete demonstration of all spec features.

---

## Phase 12: Documentation

### Step 26–30 — Update docs

| File | Changes |
|------|---------|
| `docs/CORE_API.md` | New `SkillResource` types, `ResolveFileReferences`, `ReadResource` APIs |
| `docs/SKILL_FORMAT.md` | Optional directories and file references |
| `docs/CLI.md` | New `list-resources`, `read-resource`, `check-references` commands |
| `docs/ARCHITECTURE.md` | Script execution pipeline design |
| `README.md` | Update roadmap table — change "Planned" to "Implemented" |

---

## Verification

```bash
dotnet build AgentSkills.slnx
dotnet test
dotnet run --project src/elbruno.Extensions.AI.Skills.Cli -- validate ./samples/skills/code-review
dotnet run --project src/elbruno.Extensions.AI.Skills.Cli -- list-resources ./samples/skills/code-review
dotnet run --project src/elbruno.Extensions.AI.Skills.Cli -- to-prompt ./samples/skills/code-review
```

Manually verify the `BasicSkillUsage` / `AgentWithSkills` sample projects still work.

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| **Unified `SkillResource` type** vs separate `ScriptInfo`/`ReferenceInfo`/`AssetInfo` | Simpler API surface, extensible, follows the spec's generic "resources" concept |
| **Warnings vs errors** for sub-directory validation | Sub-directory issues are warnings (not blocking errors) since the spec makes these directories optional |
| **File enumeration depth**: one level | Per spec guidance: "Keep file references one level deep from SKILL.md" |
| **Script execution gating** via existing options | `TrustedSkills` and `RequireConfirmation` already declared in `SkillsOptions` — no new config surface needed |
| **Resource content not in prompt XML** | Only file names in the prompt, not content. Matches progressive disclosure — agents load content on demand via `ReadResource` |
