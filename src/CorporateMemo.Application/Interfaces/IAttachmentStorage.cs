namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Defines the contract for storing and retrieving file attachments.
/// This interface abstracts the underlying storage mechanism (local file system, Azure Blob, S3, etc.).
/// The Application layer uses this interface without knowing how files are actually stored.
/// </summary>
public interface IAttachmentStorage
{
    /// <summary>
    /// Saves a file stream to the storage system and returns a reference path.
    /// The returned path is stored in the Attachment entity and used later to retrieve the file.
    /// </summary>
    /// <param name="stream">The file's data as a stream. The stream must be readable.</param>
    /// <param name="fileName">
    /// The original file name (e.g., "report.pdf").
    /// The implementation is responsible for sanitising this name before storage.
    /// </param>
    /// <param name="contentType">
    /// The MIME content type of the file (e.g., "application/pdf", "image/png").
    /// Used to set Content-Type headers when the file is downloaded.
    /// </param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>
    /// The storage path where the file was saved.
    /// This path is relative to the storage root and is stored in the Attachment.StoragePath field.
    /// </returns>
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a file as a readable stream from the storage system.
    /// </summary>
    /// <param name="storagePath">
    /// The path returned by <see cref="SaveAsync"/> when the file was uploaded.
    /// </param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>A readable Stream containing the file's data.</returns>
    Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a file from the storage system.
    /// Called when an attachment is removed from a memo or the memo itself is deleted.
    /// </summary>
    /// <param name="storagePath">
    /// The path returned by <see cref="SaveAsync"/> when the file was uploaded.
    /// </param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
