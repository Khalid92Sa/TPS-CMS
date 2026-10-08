using System;

namespace CMS.Domain.Entities;

/// <summary>
/// Temporary, interview-specific permission to submit a score.
/// The original interviewer assignment on the interview row is not changed.
/// </summary>
public class InterviewDelegation : BaseEntity
{
    public const string StatusActive = "Active";
    public const string StatusCancelled = "Cancelled";

    /// <summary>The delegate submitted the score while the delegation was in force.</summary>
    public const string StatusCompleted = "Completed";

    /// <summary>The interview was scored by someone else while this delegation was still open.</summary>
    public const string StatusClosed = "Closed";

    public const string SlotPrimary = "Primary";
    public const string SlotSecond = "Second";

    /// <summary>
    /// ExpirationDate stored for a delegation created from an unlimited duration. A far-future date
    /// (rather than NULL) keeps every "ExpirationDate > now" check working unchanged.
    /// </summary>
    public static readonly DateTime NoExpirationDate = new(9999, 12, 31);

    public static bool IsNoExpiration(DateTime expirationDate) => expirationDate >= NoExpirationDate;

    public int Id { get; set; }

    public int InterviewId { get; set; }

    public int CandidateId { get; set; }

    public string OriginalInterviewerId { get; set; }

    public string DelegatedUserId { get; set; }

    public string DelegatedByUserId { get; set; }

    public string RemovedByUserId { get; set; }

    public DateTime? RemovedOn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public bool IsUnlimited => IsNoExpiration(ExpirationDate);

    /// <summary>
    /// Name of the duration option at the time the delegation was created.
    /// </summary>
    public string DurationName { get; set; }

    public int? DurationMinutes { get; set; }

    public int? WorkflowStageId { get; set; }

    /// <summary>
    /// Primary = Interviews.InterviewerId, Second = Interviews.SecondInterviewerId.
    /// </summary>
    public string InterviewerSlot { get; set; }

    /// <summary>
    /// Active while the delegation is open. Cancelled when removed, Completed when the
    /// delegate submitted the score, Closed when someone else scored the interview first.
    /// Expired and not-started states are derived from StartDate and ExpirationDate.
    /// While an Active delegation is in force, both the delegate and the original
    /// interviewer of that slot may submit the score; the first submission wins.
    /// </summary>
    public string Status { get; set; }
}
