# CLI Tool

The `elbruno.Extensions.AI.Skills.Cli` package provides a .NET global tool for validating skills and generating prompt XML from the command line.

## Install

```bash
dotnet tool install -g elbruno.Extensions.AI.Skills.Cli
```

## Commands

### Validate a skill

```bash
skills validate ./my-skill
```

### Read properties as JSON

```bash
skills read-properties ./my-skill
```

### Generate `<available_skills>` XML

```bash
skills to-prompt ./skill-a ./skill-b
```
