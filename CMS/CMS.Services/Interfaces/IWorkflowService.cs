using CMS.Application.DTOs;
using CMS.Application.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Services.Interfaces;

public interface IWorkflowService
{
    Task<Result<List<NextStageInfoDTO>>> GetNextStagesAsync(int currentInterviewId, string currentUserRole, int? positionId = null, int? trackId = null);
    Task<Result<WorkflowStageDTO>> GetStageByIdAsync(int stageId);
    Task<Result<List<WorkflowStageDTO>>> GetAllStagesAsync();
    Task<Result<WorkflowConfigurationDTO>> GetConfigurationByRoleAndStageAsync(int stageId, string roleName, int? positionId = null, int? trackId = null);
    Task<Result<bool>> CanRoleConductInterviewAtStageAsync(int stageId, string roleName, int? positionId = null, int? trackId = null);
    Task<Result<bool>> IsFinalStageAsync(int stageId);
    Task<Result<WorkflowStageDTO>> CreateStageAsync(WorkflowStageDTO stage);
    Task<Result<WorkflowConfigurationDTO>> CreateConfigurationAsync(WorkflowConfigurationDTO configuration);
}

