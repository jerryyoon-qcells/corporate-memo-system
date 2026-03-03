namespace CorporateMemo.Application.DTOs;

/// <summary>
/// Data Transfer Object for file attachments.
/// This is a simplified, read-only representation of the Attachment entity
/// that is safe to pass between layers and to the UI.
/// </summary>
public class AttachmentDto
{
    /// <summary>Gets or sets the unique identifier of the attachment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the memo this attachment belongs to.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets the original file name as uploaded by the user.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME content type (e.g., "application/pdf").</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Gets the file size as a human-readable string (e.g., "2.4 MB", "512 KB").
    /// This is a computed property — it is not stored in the database.
    /// </summary>
    public string FileSizeDisplay => FileSizeBytes switch
    {
        // Format bytes as KB if less than 1 MB
        < 1_048_576 => $"{FileSizeBytes / 1024.0:F1} KB",
        // Format bytes as MB if less than 1 GB
        < 1_073_741_824 => $"{FileSizeBytes / 1_048_576.0:F1} MB",
        // Format bytes as GB for very large files
        _ => $"{FileSizeBytes / 1_073_741_824.0:F1} GB"
    };

    /// <summary>Gets or sets the storage path used to retrieve the file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when this file was uploaded.</summary>
    public DateTime UploadedAt { get; set; }
}
