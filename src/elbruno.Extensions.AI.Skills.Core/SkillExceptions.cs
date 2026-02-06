namespace elbruno.Extensions.AI.Skills.Core;

/// <summary>
/// Base exception for all skill-related errors.
/// </summary>
public class SkillException : Exception
{
    public SkillException(string message) : base(message) { }
    public SkillException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when SKILL.md parsing fails (missing file, invalid YAML, etc.).
/// </summary>
public class SkillParseException : SkillException
{
    public SkillParseException(string message) : base(message) { }
    public SkillParseException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Raised when skill properties fail validation.
/// </summary>
public class SkillValidationException : SkillException
{
    /// <summary>
    /// Individual validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    public SkillValidationException(string message)
        : base(message)
    {
        Errors = [message];
    }

    public SkillValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
