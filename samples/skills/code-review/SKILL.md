---
name: code-review
description: Reviews C# code for bugs, security issues, and best practices.
license: MIT
compatibility: Requires C# source files
allowed-tools: Read
metadata:
  author: elbruno
---
# Code Review

## When to use
Use this skill when the user asks you to review C# code, check for bugs, or audit for security issues.

## Review Checklist

### Bug Patterns
- Null reference risks (missing null checks, nullable misuse)
- Resource leaks (undisposed IDisposable objects)
- Off-by-one errors in loops and indexing
- Unhandled exceptions or swallowed catch blocks

### Security Issues
- SQL injection (string concatenation in queries)
- Hardcoded secrets or connection strings
- Path traversal vulnerabilities
- Insecure deserialization

### Best Practices
- Follow C# naming conventions (PascalCase for public, camelCase for private)
- Use `async/await` properly (avoid `.Result`, `.Wait()`)
- Prefer `IReadOnlyList<T>` over `List<T>` in public APIs
- Use pattern matching and modern C# features

## Output Format
Provide a summary with severity levels: **Critical**, **Warning**, **Info**.
