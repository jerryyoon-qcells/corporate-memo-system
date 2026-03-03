namespace CorporateMemo.Domain.Entities;

/// <summary>
/// Represents a file attachment associated with a memo.
/// Attachments are stored on the file system and referenced by their storage path.
/// </summary>
public class Attachment
{
    /// <summary>
    /// Gets or sets the unique identifier for this attachment.
    /// A new Guid is assigned automatically when the attachment is created.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the memo this attachment belongs to.
    /// This is a foreign key linking back to the Memo entity.
    /// </summary>
    public Guid MemoId { get; set; }

    /// <summary>
    /// Gets or sets the original file name as uploaded by the user.
    /// This name is sanitised before storage to prevent path traversal attacks.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME content type of the file (e.g., "application/pdf", "image/png").
    /// Used to set the correct Content-Type header when the file is downloaded.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// Used to display human-readable file sizes in the UI (e.g., "2.4 MB").
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the path where the file is stored on the server's file system.
    /// This path is relative to the configured upload directory, not the web root.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this file was uploaded.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: allows EF Core to load the related Memo in the same query
    /// <summary>
    /// Gets or sets the memo that owns this attachment.
    /// Navigation property used by Entity Framework Core for related data loading.
    /// </summary>
    public Memo? Memo { get; set; }
}
