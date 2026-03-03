using CorporateMemo.Application.Interfaces;

namespace CorporateMemo.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration class for file attachment settings.
/// Bound to the "AttachmentSettings" section in appsettings.json.
/// Controls where files are stored, how large they can be, and which types are allowed.
///
/// Implements <see cref="IAttachmentSettings"/> so that Application-layer validators
/// can access size and extension constraints without referencing this Infrastructure class.
/// </summary>
public class AttachmentSettings : IAttachmentSettings
{
    /// <summary>
    /// Gets or sets the file system path where uploaded attachments are stored.
    /// Use Path.Combine to ensure cross-platform compatibility (no hardcoded path separators).
    /// Default: "attachments" — a folder at the same level as wwwroot, NOT inside it.
    /// Storing files inside wwwroot would make them directly accessible via HTTP without
    /// authentication. Files must be served only through the authenticated AttachmentsController.
    /// </summary>
    public string UploadPath { get; set; } = "attachments";

    /// <summary>
    /// Gets or sets the maximum allowed file size per attachment in megabytes.
    /// Default: 10 MB per the requirements.
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 10;

    /// <summary>
    /// Gets the maximum file size in bytes (computed from MaxFileSizeMb).
    /// Used for validation without manual MB-to-bytes conversion at call sites.
    /// </summary>
    public long MaxFileSizeBytes => (long)MaxFileSizeMb * 1024 * 1024;

    /// <summary>
    /// Gets or sets the list of allowed file extensions (without leading dot).
    /// Example: ["pdf", "docx", "xlsx", "png", "jpg", "jpeg", "gif", "bmp"]
    /// Files with extensions not in this list will be rejected.
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        "pdf", "docx", "xlsx", "png", "jpg", "jpeg", "gif", "bmp"
    };

    // IAttachmentSettings explicit implementation — exposes the list as IReadOnlyList
    IReadOnlyList<string> IAttachmentSettings.AllowedExtensions => AllowedExtensions.AsReadOnly();
}
