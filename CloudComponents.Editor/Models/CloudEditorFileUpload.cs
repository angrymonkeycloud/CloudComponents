namespace AngryMonkey.CloudComponents.Editor.Models;

/// <summary>
/// Describes a file picked by the user for insertion into the editor.
/// Passed to <c>CloudEditor.ImageUploadHandler</c> so the host application
/// can persist the file and return its public URL.
/// </summary>
public sealed class CloudEditorFileUpload
{
    /// <summary>Original file name including extension.</summary>
    public required string FileName { get; init; }

    /// <summary>MIME content type reported by the browser.</summary>
    public required string ContentType { get; init; }

    /// <summary>File size in bytes.</summary>
    public required long Size { get; init; }

    /// <summary>
    /// Opens the file content for reading. The stream is limited to the
    /// maximum upload size configured on the editor.
    /// </summary>
    public required Func<Stream> OpenReadStream { get; init; }
}
