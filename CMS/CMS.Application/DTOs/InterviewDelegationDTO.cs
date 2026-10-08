using CMS.Domain.Entities;
using System;
using System.Collections.Generic;

namespace CMS.Application.DTOs;

public class InterviewDelegationDTO
{
    public int InterviewId { get; set; }

    public int CandidateId { get; set; }

    public string CandidateName { get; set; }

    public int PositionId { get; set; }

    public string PositionName { get; set; }

    public string StageName { get; set; }

    public DateTime InterviewDate { get; set; }

    public int? StatusId { get; set; }

    public string OriginalInterviewerId { get; set; }

    public string OriginalInterviewerName { get; set; }

    public string DelegatedUserId { get; set; }

    public string DelegatedUserName { get; set; }

    public string DelegatedByUserId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public bool IsUnlimited => InterviewDelegation.IsNoExpiration(ExpirationDate);

    public int? WorkflowStageId { get; set; }

    public string InterviewerSlot { get; set; }

    /// <summary>InterviewDelegation.Status* value (Active, Completed, ...).</summary>
    public string Status { get; set; }
}

public class InterviewDelegationHistoryItemDTO
{
    public int Id { get; set; }

    public string OriginalInterviewerName { get; set; }

    public string DelegatedUserName { get; set; }

    public string DelegatedByName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public bool IsUnlimited => InterviewDelegation.IsNoExpiration(ExpirationDate);

    public DateTime CreatedOn { get; set; }

    public string Duration { get; set; }

    public string InterviewerSlot { get; set; }

    public string Status { get; set; }

    public bool CanRemove { get; set; }

    public string RemovedByName { get; set; }

    public DateTime? RemovedOn { get; set; }

    public string DelegatedUserId { get; set; }

    public int InterviewId { get; set; }

    /// <summary>When the delegation reached its final state (removed, completed or closed).</summary>
    public DateTime? EndedOn { get; set; }

    /// <summary>True while the delegation still controls who may submit the score.</summary>
    public bool IsOpen => Status == "Active" || Status == "Not started";
}

public class DelegateInterviewPageDTO
{
    public int InterviewId { get; set; }

    public string CandidateName { get; set; }

    public string PositionName { get; set; }

    public string StageName { get; set; }

    public DateTime InterviewDate { get; set; }

    public string StatusName { get; set; }

    public string PrimaryInterviewerId { get; set; }

    public string PrimaryInterviewerName { get; set; }

    public string PrimaryRole { get; set; }

    public string SecondInterviewerId { get; set; }

    public string SecondInterviewerName { get; set; }

    public string SecondRole { get; set; }

    public List<DelegationDurationDTO> Durations { get; set; } = [];

    public List<UsersDTO> DelegateCandidates { get; set; } = [];

    public List<InterviewDelegationHistoryItemDTO> History { get; set; } = [];

    /// <summary>False once the interview is scored: the page is then read-only history.</summary>
    public bool IsPending { get; set; }

    public InterviewDelegationHistoryItemDTO ActivePrimary { get; set; }

    public InterviewDelegationHistoryItemDTO ActiveSecond { get; set; }
}

public class BulkDelegationPageDTO
{
    public List<InterviewsDTO> Interviews { get; set; } = [];

    /// <summary>Open delegations keyed by interview id.</summary>
    public Dictionary<int, List<InterviewDelegationHistoryItemDTO>> OpenDelegations { get; set; } = [];

    public List<UsersDTO> DelegateCandidates { get; set; } = [];

    public List<DelegationDurationDTO> Durations { get; set; } = [];
}

public class BulkDelegationResultDTO
{
    public List<InterviewDelegationDTO> Delegated { get; set; } = [];

    public List<string> Errors { get; set; } = [];
}
