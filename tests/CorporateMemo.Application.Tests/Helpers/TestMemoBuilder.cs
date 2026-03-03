using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.Tests.Helpers;

/// <summary>
/// Builder helper that creates Memo entities pre-populated with sensible test defaults.
/// This avoids duplicating the same boilerplate entity construction across every test class.
///
/// Usage:
///   var memo = TestMemoBuilder.Create().WithStatus(MemoStatus.Draft).Build();
/// </summary>
public class TestMemoBuilder
{
    // ---- defaults ----
    private Guid _id = Guid.NewGuid();
    private string _memoNumber = "testuser-20260302-001";
    private string _title = "Test Memo Title";
    private string _content = "Test memo content body.";
    private string _authorId = "author-user-id";
    private string _authorName = "Test Author";
    private string _authorEmail = "author@example.com";
    private MemoStatus _status = MemoStatus.Draft;
    private List<string> _toRecipients = new() { "recipient@example.com" };
    private List<string> _ccRecipients = new();
    private List<string> _tags = new();
    private List<ApprovalStep> _approvalSteps = new();
    private bool _isConfidential;

    private TestMemoBuilder() { }

    /// <summary>Creates a new builder instance with default values.</summary>
    public static TestMemoBuilder Create() => new();

    /// <summary>Sets the memo ID.</summary>
    public TestMemoBuilder WithId(Guid id) { _id = id; return this; }

    /// <summary>Sets the memo status.</summary>
    public TestMemoBuilder WithStatus(MemoStatus status) { _status = status; return this; }

    /// <summary>Sets the author user ID.</summary>
    public TestMemoBuilder WithAuthorId(string authorId) { _authorId = authorId; return this; }

    /// <summary>Sets the To recipients list.</summary>
    public TestMemoBuilder WithToRecipients(params string[] recipients)
    {
        _toRecipients = recipients.ToList();
        return this;
    }

    /// <summary>Clears the To recipients so the memo has none (for validation testing).</summary>
    public TestMemoBuilder WithNoRecipients() { _toRecipients = new(); return this; }

    /// <summary>Adds a single approval step for the given approver ID.</summary>
    public TestMemoBuilder WithApprover(string approverId, string approverName = "Approver Name",
        string approverEmail = "approver@example.com", ApprovalDecision decision = ApprovalDecision.Pending)
    {
        _approvalSteps.Add(new ApprovalStep
        {
            Id = Guid.NewGuid(),
            MemoId = _id,
            ApproverId = approverId,
            ApproverName = approverName,
            ApproverEmail = approverEmail,
            Order = _approvalSteps.Count + 1,
            Decision = decision
        });
        return this;
    }

    /// <summary>Sets all approval steps at once (replaces any previously added).</summary>
    public TestMemoBuilder WithApprovalSteps(List<ApprovalStep> steps) { _approvalSteps = steps; return this; }

    /// <summary>Marks the memo as confidential.</summary>
    public TestMemoBuilder AsConfidential() { _isConfidential = true; return this; }

    /// <summary>Builds the Memo entity from the accumulated configuration.</summary>
    public Memo Build() => new()
    {
        Id = _id,
        MemoNumber = _memoNumber,
        Title = _title,
        Content = _content,
        AuthorId = _authorId,
        AuthorName = _authorName,
        AuthorEmail = _authorEmail,
        DateCreated = DateTime.UtcNow,
        Status = _status,
        ToRecipients = _toRecipients,
        CcRecipients = _ccRecipients,
        Tags = _tags,
        ApprovalSteps = _approvalSteps,
        IsConfidential = _isConfidential,
        Attachments = new List<Attachment>()
    };
}
