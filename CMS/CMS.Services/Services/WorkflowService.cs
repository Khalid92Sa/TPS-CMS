using CMS.Application.DTOs;
using CMS.Application.Extensions;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Services.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IInterviewsRepository _interviewsRepository;
    private readonly UserManager<IdentityUser> _userManager;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IInterviewsRepository interviewsRepository,
        UserManager<IdentityUser> userManager)
    {
        _workflowRepository = workflowRepository;
        _interviewsRepository = interviewsRepository;
        _userManager = userManager;
    }

    public async Task<Result<List<NextStageInfoDTO>>> GetNextStagesAsync(int currentInterviewId, string currentUserRole, int? positionId = null, int? trackId = null)
    {
        try
        {
            var currentInterview = await _interviewsRepository.GetById(currentInterviewId);
            if (currentInterview == null)
                return Result<List<NextStageInfoDTO>>.Failure(null, "Interview not found");

            // Determine current stage based on ParentId
            int? currentStageId = null;
            if (currentInterview.ParentId == null)
            {
                // First interview - get first stage
                var firstStage = await _workflowRepository.GetFirstStageAsync();
                currentStageId = firstStage?.Id;
            }
            else
            {
                // Find the stage based on the interview's position in the hierarchy
                // This is a simplified approach - you might want to store StageId in Interviews entity
                var parentInterview = await _interviewsRepository.GetById(currentInterview.ParentId.Value);
                if (parentInterview != null)
                {
                    // Get configuration for parent interview's role
                    var parentConfig = await _workflowRepository.GetConfigurationByStageAndRoleAsync(
                        currentStageId ?? 1, // This needs to be tracked properly
                        await GetRoleNameAsync(parentInterview.InterviewerId),
                        positionId,
                        trackId);

                    if (parentConfig?.NextStageId != null)
                        currentStageId = parentConfig.NextStageId;
                }
            }

            if (!currentStageId.HasValue)
                return Result<List<NextStageInfoDTO>>.Failure(null, "Could not determine current stage");

            // Get configurations for current stage
            var configurations = await _workflowRepository.GetConfigurationsByStageIdAsync(
                currentStageId.Value,
                positionId,
                trackId);

            // Filter by current user's role
            var matchingConfig = configurations.FirstOrDefault(c => c.RoleName == currentUserRole);
            if (matchingConfig == null)
                return Result<List<NextStageInfoDTO>>.Failure(null, $"No workflow configuration found for role: {currentUserRole}");

            var nextStages = new List<NextStageInfoDTO>();

            // If there's a next stage, get its information
            if (matchingConfig.NextStageId.HasValue)
            {
                var nextStage = await _workflowRepository.GetStageByIdAsync(matchingConfig.NextStageId.Value);
                if (nextStage != null)
                {
                    // Get all configurations for the next stage
                    var nextStageConfigs = await _workflowRepository.GetConfigurationsByStageIdAsync(
                        nextStage.Id,
                        positionId,
                        trackId);

                    if (nextStage.IsParallelStage)
                    {
                        // Create separate entries for each parallel configuration
                        foreach (var config in nextStageConfigs)
                        {
                            nextStages.Add(new NextStageInfoDTO
                            {
                                StageId = nextStage.Id,
                                StageName = nextStage.Name,
                                RequiredRoles = new List<string> { config.RoleName },
                                IsParallelStage = true,
                                RequiresSecondInterviewer = config.RequiresSecondInterviewer,
                                SecondInterviewerRoleName = config.SecondInterviewerRoleName
                            });
                        }
                    }
                    else
                    {
                        // Single next stage
                        nextStages.Add(new NextStageInfoDTO
                        {
                            StageId = nextStage.Id,
                            StageName = nextStage.Name,
                            RequiredRoles = nextStageConfigs.Select(c => c.RoleName).ToList(),
                            IsParallelStage = false,
                            RequiresSecondInterviewer = matchingConfig.RequiresSecondInterviewer,
                            SecondInterviewerRoleName = matchingConfig.SecondInterviewerRoleName
                        });
                    }
                }
            }

            return Result<List<NextStageInfoDTO>>.Success(nextStages);
        }
        catch (System.Exception ex)
        {
            return Result<List<NextStageInfoDTO>>.Failure(null, $"Error getting next stages: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStageDTO>> GetStageByIdAsync(int stageId)
    {
        try
        {
            var stage = await _workflowRepository.GetStageByIdAsync(stageId);
            if (stage == null)
                return Result<WorkflowStageDTO>.Failure(null, "Stage not found");

            var dto = MapToStageDTO(stage);
            return Result<WorkflowStageDTO>.Success(dto);
        }
        catch (System.Exception ex)
        {
            return Result<WorkflowStageDTO>.Failure(null, $"Error getting stage: {ex.Message}");
        }
    }

    public async Task<Result<List<WorkflowStageDTO>>> GetAllStagesAsync()
    {
        try
        {
            var stages = await _workflowRepository.GetAllStagesAsync();
            var dtos = stages.Select(MapToStageDTO).ToList();
            return Result<List<WorkflowStageDTO>>.Success(dtos);
        }
        catch (System.Exception ex)
        {
            return Result<List<WorkflowStageDTO>>.Failure(null, $"Error getting stages: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowConfigurationDTO>> GetConfigurationByRoleAndStageAsync(int stageId, string roleName, int? positionId = null, int? trackId = null)
    {
        try
        {
            var config = await _workflowRepository.GetConfigurationByStageAndRoleAsync(stageId, roleName, positionId, trackId);
            if (config == null)
                return Result<WorkflowConfigurationDTO>.Failure(null, "Configuration not found");

            var dto = MapToConfigurationDTO(config);
            return Result<WorkflowConfigurationDTO>.Success(dto);
        }
        catch (System.Exception ex)
        {
            return Result<WorkflowConfigurationDTO>.Failure(null, $"Error getting configuration: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CanRoleConductInterviewAtStageAsync(int stageId, string roleName, int? positionId = null, int? trackId = null)
    {
        try
        {
            var config = await _workflowRepository.GetConfigurationByStageAndRoleAsync(stageId, roleName, positionId, trackId);
            return Result<bool>.Success(config != null);
        }
        catch (System.Exception ex)
        {
            return Result<bool>.Failure(false, $"Error checking role permission: {ex.Message}");
        }
    }

    public async Task<Result<bool>> IsFinalStageAsync(int stageId)
    {
        try
        {
            var stage = await _workflowRepository.GetStageByIdAsync(stageId);
            if (stage == null)
                return Result<bool>.Failure(false, "Stage not found");

            return Result<bool>.Success(stage.IsFinalStage);
        }
        catch (System.Exception ex)
        {
            return Result<bool>.Failure(false, $"Error checking final stage: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowStageDTO>> CreateStageAsync(WorkflowStageDTO stageDTO)
    {
        try
        {
            var stage = new WorkflowStage
            {
                Name = stageDTO.Name,
                StageOrder = stageDTO.StageOrder,
                Description = stageDTO.Description,
                IsFinalStage = stageDTO.IsFinalStage,
                IsParallelStage = stageDTO.IsParallelStage,
                CreatedOn = System.DateTime.Now,
                IsActive = true,
                IsDelete = false
            };

            var created = await _workflowRepository.CreateStageAsync(stage);
            return Result<WorkflowStageDTO>.Success(MapToStageDTO(created));
        }
        catch (System.Exception ex)
        {
            return Result<WorkflowStageDTO>.Failure(null, $"Error creating stage: {ex.Message}");
        }
    }

    public async Task<Result<WorkflowConfigurationDTO>> CreateConfigurationAsync(WorkflowConfigurationDTO configDTO)
    {
        try
        {
            var config = new WorkflowConfiguration
            {
                WorkflowStageId = configDTO.WorkflowStageId,
                RoleName = configDTO.RoleName,
                NextStageId = configDTO.NextStageId,
                RequiresSecondInterviewer = configDTO.RequiresSecondInterviewer,
                SecondInterviewerRoleName = configDTO.SecondInterviewerRoleName,
                CanStopWorkflow = configDTO.CanStopWorkflow,
                PositionId = configDTO.PositionId,
                TrackId = configDTO.TrackId,
                CreatedOn = System.DateTime.Now,
                IsActive = true,
                IsDelete = false
            };

            var created = await _workflowRepository.CreateConfigurationAsync(config);
            return Result<WorkflowConfigurationDTO>.Success(MapToConfigurationDTO(created));
        }
        catch (System.Exception ex)
        {
            return Result<WorkflowConfigurationDTO>.Failure(null, $"Error creating configuration: {ex.Message}");
        }
    }

    private async Task<string> GetRoleNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    private WorkflowStageDTO MapToStageDTO(WorkflowStage stage)
    {
        return new WorkflowStageDTO
        {
            Id = stage.Id,
            Name = stage.Name,
            StageOrder = stage.StageOrder,
            Description = stage.Description,
            IsFinalStage = stage.IsFinalStage,
            IsParallelStage = stage.IsParallelStage,
            Configurations = stage.WorkflowConfigurations?.Select(MapToConfigurationDTO).ToList() ?? new List<WorkflowConfigurationDTO>()
        };
    }

    private WorkflowConfigurationDTO MapToConfigurationDTO(WorkflowConfiguration config)
    {
        return new WorkflowConfigurationDTO
        {
            Id = config.Id,
            WorkflowStageId = config.WorkflowStageId,
            StageName = config.WorkflowStage?.Name,
            RoleName = config.RoleName,
            NextStageId = config.NextStageId,
            NextStageName = config.NextStage?.Name,
            RequiresSecondInterviewer = config.RequiresSecondInterviewer,
            SecondInterviewerRoleName = config.SecondInterviewerRoleName,
            CanStopWorkflow = config.CanStopWorkflow,
            PositionId = config.PositionId,
            PositionName = config.Position?.Name,
            TrackId = config.TrackId,
            TrackName = config.Track?.Name
        };
    }
}

