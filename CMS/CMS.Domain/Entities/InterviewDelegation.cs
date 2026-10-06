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
    /// Active while the delegation has not been cancelled. Expired and not-started
    /// states are derived from StartDate and ExpirationDate.
    /// </summary>
    public string Status { get; set; }
}
