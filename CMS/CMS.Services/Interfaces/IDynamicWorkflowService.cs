using CMS.Application.Extensions;
using System;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

/// <summary>
/// Service for handling dynamic workflow progression
/// </summary>
public interface IDynamicWorkflowService
{
    /// <summary>
    /// Creates next stage interviews based on completed interview
    /// </summary>
    Task<Result<bool>> CreateNextStageInterviewsAsync(
        int completedInterviewId,
        string completedByRole,
        int candidateId,
        int positionId,
        int trackId,
        DateTime interviewDate,
        string createdByUserId);
}

