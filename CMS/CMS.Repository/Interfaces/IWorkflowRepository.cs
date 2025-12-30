using CMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Repository.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowStage> GetStageByIdAsync(int stageId);
    Task<WorkflowStage> GetFirstStageAsync();
    Task<List<WorkflowConfiguration>> GetConfigurationsByStageIdAsync(int stageId, int? positionId = null, int? trackId = null);
    Task<WorkflowConfiguration> GetConfigurationByStageAndRoleAsync(int stageId, string roleName, int? positionId = null, int? trackId = null);
    Task<List<WorkflowStage>> GetAllStagesAsync();
    Task<List<WorkflowConfiguration>> GetAllConfigurationsAsync();
    Task<WorkflowStage> CreateStageAsync(WorkflowStage stage);
    Task<WorkflowConfiguration> CreateConfigurationAsync(WorkflowConfiguration configuration);
    Task<WorkflowStage> UpdateStageAsync(WorkflowStage stage);
    Task<WorkflowConfiguration> UpdateConfigurationAsync(WorkflowConfiguration configuration);
    Task<bool> DeleteStageAsync(int stageId);
    Task<bool> DeleteConfigurationAsync(int configurationId);
}

