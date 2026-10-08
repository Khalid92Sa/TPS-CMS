using System;

namespace CMS.Application.DTOs;

/// <summary>
/// Filters for the interview list pages (Interviews/Index and SearchInterviews/Index).
/// Every filter is optional; unset values do not filter. The query runs in the database.
/// </summary>
public class InterviewListFilter
{
    public int? StatusId { get; set; }

    public int? TrackId { get; set; }

    public int? PositionId { get; set; }

    public double? Score { get; set; }

    /// <summary>Matches the primary or the second interviewer.</summary>
    public string InterviewerId { get; set; }

    /// <summary>Part of the candidate's full name (case-insensitive under the database collation).</summary>
    public string CandidateName { get; set; }

    /// <summary>Interviews on or after this day (time of day ignored).</summary>
    public DateTime? FromDay { get; set; }

    /// <summary>Inclusive lower bound on the interview date and time.</summary>
    public DateTime? From { get; set; }

    /// <summary>Inclusive upper bound on the interview date and time.</summary>
    public DateTime? To { get; set; }
}
