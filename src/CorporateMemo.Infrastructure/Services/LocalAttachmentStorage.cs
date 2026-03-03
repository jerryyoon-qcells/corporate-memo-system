using CorporateMemo.Application.Interfaces;
using CorporateMemo.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CorporateMemo.Infrastructure.Services;

/// <summary>
/// Local file system implementation of the <see cref="IAttachmentStorage"/> interface.
/// Stores uploaded files in a configurable directory on the server's file system.
///
/// For production, consider replacing this with Azure Blob Storage or similar
/// cloud storage for better durability and scalability.
///
/// File names are sanitised to prevent path traversal attacks (e.g., "../../etc/passwd").
/// Files are stored with a GUID prefix to avoid naming collisions.
///
/// C2 fix: The upload directory is resolved relative to <see cref="AppContext.BaseDirectory"/>
/// (the application's binary output folder), NOT relative to wwwroot. This means uploaded
/// files are not directly accessible via HTTP — they can only be served through the
/// authenticated <c>AttachmentsController</c>.
///
/// M6 fix: File I/O uses FileStream with FileOptions.Asynchronous and async read/write methods
/// so that threads are not blocked during disk operations under high concurrency.
/// </summary>
public class LocalAttachmentStorage : IAttachmentStorage
{
    private readonly AttachmentSettings _settings;
    private readonly ILogger<LocalAttachmentStorage> _logger;

    /// <summary>The fully resolved absolute path to the upload directory.</summary>
    private readonly string _resolvedUploadPath;

    /// <summary>
    /// Initializes the storage service with attachment configuration.
    /// Also ensures the upload directory exists on startup.
    /// </summary>
    public LocalAttachmentStorage(IOptions<AttachmentSettings> settings, ILogger<LocalAttachmentStorage> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // C2 fix: Resolve the upload path relative to AppContext.BaseDirectory so it sits
        // OUTSIDE the web root. If UploadPath is already absolute (e.g. set via environment
        // variable), Path.Combine returns it unchanged.
        _resolvedUploadPath = Path.IsPathRooted(_settings.UploadPath)
            ? _settings.UploadPath
            : Path.Combine(AppContext.BaseDirectory, _settings.UploadPath);

        // Ensure the upload directory exists — create it if it doesn't
        // This prevents "directory not found" errors on first upload
        if (!Directory.Exists(_resolvedUploadPath))
        {
            Directory.CreateDirectory(_resolvedUploadPath);
            _logger.LogInformation("Created upload directory: {Path}", _resolvedUploadPath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        // C1 defence-in-depth: enforce extension and size checks even if the validator
        // was somehow bypassed (e.g., direct infrastructure usage in tests or scripts).
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrEmpty(ext) &&
            _settings.AllowedExtensions.Count > 0 &&
            !_settings.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Storage refused to save file '{fileName}': extension '.{ext}' is not in the allowed list.");
        }

        if (stream.CanSeek && stream.Length > _settings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"Storage refused to save file '{fileName}': size exceeds the {_settings.MaxFileSizeMb} MB limit.");
        }

        // Sanitise the file name to prevent path traversal attacks
        // Path.GetFileName strips any directory components (e.g., "../../../etc/passwd" → "passwd")
        var sanitisedFileName = Path.GetFileName(fileName);

        // Remove any characters that could cause issues on Windows or Linux file systems
        var safeName = string.Concat(sanitisedFileName.Split(Path.GetInvalidFileNameChars()));

        if (string.IsNullOrEmpty(safeName))
            safeName = "attachment";

        // Prefix with a GUID to ensure uniqueness even if two users upload files with the same name
        var uniqueFileName = $"{Guid.NewGuid():N}_{safeName}";

        // Use Path.Combine for cross-platform compatibility (forward slash on Linux, backslash on Windows)
        var fullPath = Path.Combine(_resolvedUploadPath, uniqueFileName);

        _logger.LogDebug("Saving attachment to: {Path}", fullPath);

        // M6 fix: Open the file stream with FileOptions.Asynchronous so the OS uses
        // overlapped I/O; then copy using CopyToAsync for truly non-blocking disk writes.
        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            options: FileOptions.Asynchronous);

        await stream.CopyToAsync(fileStream, ct);

        _logger.LogInformation("Saved attachment: {OriginalName} → {StoredPath}", fileName, uniqueFileName);

        // Return only the file name (not the full path) as the storage path
        // This keeps the stored path portable — if the upload directory changes, the path still works
        return uniqueFileName;
    }

    /// <inheritdoc/>
    public async Task<Stream> GetStreamAsync(string storagePath, CancellationToken ct = default)
    {
        // Reconstruct the full path from the stored file name
        var fullPath = Path.Combine(_resolvedUploadPath, storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Attachment file not found: {Path}", fullPath);
            throw new FileNotFoundException($"Attachment file not found at: {storagePath}", fullPath);
        }

        // M6 fix: Open with FileOptions.Asynchronous for non-blocking reads.
        // The caller is responsible for disposing the returned stream.
        var fileStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous);

        // Satisfy the async signature; actual I/O will be async when the caller reads the stream
        return await Task.FromResult<Stream>(fileStream);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        // Reconstruct the full path from the stored file name
        var fullPath = Path.Combine(_resolvedUploadPath, storagePath);

        if (File.Exists(fullPath))
        {
            // File deletion is a fast OS metadata operation; async wrapping adds no benefit here
            File.Delete(fullPath);
            _logger.LogDebug("Deleted attachment: {Path}", fullPath);
        }
        else
        {
            // File already gone — log a warning but don't throw (idempotent delete)
            _logger.LogWarning("Attempted to delete non-existent attachment: {Path}", fullPath);
        }

        return Task.CompletedTask;
    }
}
