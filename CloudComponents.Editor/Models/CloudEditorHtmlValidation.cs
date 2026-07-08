namespace AngryMonkey.CloudComponents.Editor.Models;

/// <summary>Severity of an HTML validation message.</summary>
public enum CloudEditorValidationSeverity
{
    /// <summary>The document is malformed and cannot be applied safely.</summary>
    Error,

    /// <summary>Suspicious but tolerated markup (for example a script tag).</summary>
    Warning
}

/// <summary>A single message produced while validating HTML source.</summary>
public sealed record CloudEditorHtmlValidationMessage(
    CloudEditorValidationSeverity Severity,
    int Line,
    int Column,
    string Message)
{
    public override string ToString() => $"{Severity} ({Line},{Column}): {Message}";
}

/// <summary>Result of validating HTML source in the editor code view.</summary>
public sealed class CloudEditorHtmlValidationResult
{
    /// <summary>All messages produced by the validator, in document order.</summary>
    public IReadOnlyList<CloudEditorHtmlValidationMessage> Messages { get; init; } = [];

    /// <summary><c>true</c> when no <see cref="CloudEditorValidationSeverity.Error"/> messages exist.</summary>
    public bool IsValid => Messages.All(message => message.Severity != CloudEditorValidationSeverity.Error);

    /// <summary>Errors only.</summary>
    public IEnumerable<CloudEditorHtmlValidationMessage> Errors =>
        Messages.Where(message => message.Severity == CloudEditorValidationSeverity.Error);

    /// <summary>Warnings only.</summary>
    public IEnumerable<CloudEditorHtmlValidationMessage> Warnings =>
        Messages.Where(message => message.Severity == CloudEditorValidationSeverity.Warning);
}
