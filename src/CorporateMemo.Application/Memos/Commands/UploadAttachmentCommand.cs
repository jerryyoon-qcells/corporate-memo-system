using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to upload a file attachment to a memo.
/// Files are saved to storage and the attachment record is linked to the memo.
/// Handled by <see cref="UploadAttachmentCommandHandler"/>.
/// </summary>
public class UploadAttachmentCommand : IRequest<AttachmentDto>
{
    /// <summary>Gets or sets the ID of the memo to attach the file to.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets the original file name as provided by the user.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIME content type of the file (e.g., "application/pdf").</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file data as a stream.
    /// The stream must be readable and positioned at the beginning.
    /// </summary>
    public Stream FileStream { get; set; } = Stream.Null;

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long FileSizeBytes { get; set; }
}
