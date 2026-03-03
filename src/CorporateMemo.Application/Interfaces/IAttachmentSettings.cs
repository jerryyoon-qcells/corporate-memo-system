namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Exposes attachment configuration that validators and handlers in the Application layer
/// need in order to enforce file size and extension rules.
///
/// This interface lives in the Application layer so that the Application layer does not
/// depend on the Infrastructure layer's <c>AttachmentSettings</c> class.
/// The Infrastructure layer's <c>AttachmentSettings</c> implements this interface so that
/// the same configured values flow from appsettings.json into both layers.
/// </summary>
public interface IAttachmentSettings
{
    /// <summary>
    /// Gets the maximum allowed file size per attachment in megabytes.
    /// </summary>
    int MaxFileSizeMb { get; }

    /// <summary>
    /// Gets the maximum file size in bytes (computed from <see cref="MaxFileSizeMb"/>).
    /// </summary>
    long MaxFileSizeBytes { get; }

    /// <summary>
    /// Gets the list of allowed file extensions (without leading dot), e.g. ["pdf","docx","png"].
    /// Files with extensions not in this list must be rejected.
    /// </summary>
    IReadOnlyList<string> AllowedExtensions { get; }
}
