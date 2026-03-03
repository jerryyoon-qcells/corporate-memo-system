using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Domain.Entities;

namespace CorporateMemo.Application.Mappings;

/// <summary>
/// AutoMapper profile that defines how Domain entities are mapped to DTOs and vice versa.
/// AutoMapper uses these mappings to automatically copy matching property values between objects.
/// This avoids writing repetitive "dto.Property = entity.Property" code throughout the codebase.
/// </summary>
public class MemoMappingProfile : Profile
{
    /// <summary>
    /// Initializes the mapping configuration.
    /// All mappings defined in this constructor are registered automatically when AutoMapper starts.
    /// </summary>
    public MemoMappingProfile()
    {
        // Map from the full Memo entity to the full MemoDto
        // AutoMapper will automatically match properties with the same name
        // The nested collections (Attachments, ApprovalSteps) are also mapped because
        // we define mappings for those types below.
        CreateMap<Memo, MemoDto>();

        // Map from the full Memo entity to the lighter-weight MemoSummaryDto
        // Only the fields present in MemoSummaryDto will be mapped; Content is excluded
        CreateMap<Memo, MemoSummaryDto>();

        // Map from ApprovalStep entity to its DTO
        CreateMap<ApprovalStep, ApprovalStepDto>();

        // Map from Attachment entity to its DTO
        CreateMap<Attachment, AttachmentDto>();

        // Map from Notification entity to its DTO
        CreateMap<Notification, NotificationDto>();
    }
}
