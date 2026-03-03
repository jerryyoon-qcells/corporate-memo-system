using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Query to retrieve a single memo by its unique identifier.
/// Returns the full MemoDto including all approval steps and attachments.
/// Handled by <see cref="GetMemoByIdQueryHandler"/>.
/// </summary>
public class GetMemoByIdQuery : IRequest<MemoDto>
{
    /// <summary>Gets or sets the unique identifier of the memo to retrieve.</summary>
    public Guid MemoId { get; set; }
}
