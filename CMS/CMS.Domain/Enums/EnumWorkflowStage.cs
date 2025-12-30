namespace CMS.Domain.Enums;

public enum EnumWorkflowStage
{
    /// <summary>
    /// Stage 1: Initial Interview (can be started by any role)
    /// </summary>
    InitialInterview = 1,

    /// <summary>
    /// Stage 2: Management Review (GM and/or Architecture)
    /// </summary>
    ManagementReview = 2,

    /// <summary>
    /// Stage 3: Final HR Interview
    /// </summary>
    FinalHRInterview = 3,

    /// <summary>
    /// Stage 4: HR Initial Interview (for reverse workflow - HR First)
    /// </summary>
    HRInitialInterview = 4,

    /// <summary>
    /// Stage 5: Interviewers Review (for reverse workflow)
    /// </summary>
    InterviewersReview = 5,

    /// <summary>
    /// Stage 6: GM Final Review (for reverse workflow)
    /// </summary>
    GMFinalReview = 6
}

