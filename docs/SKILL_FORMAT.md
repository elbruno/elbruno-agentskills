# Agent Skills Format

A skill is a directory containing a `SKILL.md` file:

```
my-skill/
├── SKILL.md          # Required: YAML frontmatter + Markdown instructions
├── scripts/          # Optional: executable code
├── references/       # Optional: additional docs
└── assets/           # Optional: templates, resources
```

## SKILL.md Example

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

## Frontmatter Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Max 64 chars. Lowercase letters, numbers, and hyphens only. Must match directory name. |
| `description` | Yes | Max 1024 chars. What the skill does and when to use it. |
| `license` | No | License name or reference to a bundled license file. |
| `compatibility` | No | Max 500 chars. Environment requirements. |
| `metadata` | No | Arbitrary key-value mapping for additional metadata. |
| `allowed-tools` | No | Space-delimited list of pre-approved tools. (Experimental) |

## Validation Rules

- `name` must be lowercase, hyphenated, max 64 chars, and match the parent directory name
- `name` must not start or end with `-`, and must not contain consecutive hyphens (`--`)
- `description` is required and max 1024 chars
- Only the fields listed above are allowed in the frontmatter

For the full specification, see [agentskills.io/specification](https://agentskills.io/specification).
