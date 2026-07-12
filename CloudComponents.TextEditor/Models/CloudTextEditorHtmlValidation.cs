namespace AngryMonkey.CloudComponents.TextEditor.Models;

/// <summary>Severity of an HTML validation message.</summary>
public enum CloudTextEditorValidationSeverity
{
    /// <summary>The document is malformed and cannot be applied safely.</summary>
    Error,

    /// <summary>Suspicious but tolerated markup (for example a script tag).</summary>
    Warning
}

/// <summary>A single message produced while validating HTML source.</summary>
public sealed record CloudTextEditorHtmlValidationMessage(
    CloudTextEditorValidationSeverity Severity,
    int Line,
    int Column,
    string Message)
{
    public override string ToString() => $"{Severity} ({Line},{Column}): {Message}";
}

/// <summary>Result of validating HTML source in the editor code view.</summary>
public sealed class CloudTextEditorHtmlValidationResult
{
    /// <summary>All messages produced by the validator, in document order.</summary>
    public IReadOnlyList<CloudTextEditorHtmlValidationMessage> Messages { get; init; } = [];

    /// <summary><c>true</c> when no <see cref="CloudTextEditorValidationSeverity.Error"/> messages exist.</summary>
    public bool IsValid => Messages.All(message => message.Severity != CloudTextEditorValidationSeverity.Error);

    /// <summary>Errors only.</summary>
    public IEnumerable<CloudTextEditorHtmlValidationMessage> Errors =>
        Messages.Where(message => message.Severity == CloudTextEditorValidationSeverity.Error);

    /// <summary>Warnings only.</summary>
    public IEnumerable<CloudTextEditorHtmlValidationMessage> Warnings =>
        Messages.Where(message => message.Severity == CloudTextEditorValidationSeverity.Warning);
}
