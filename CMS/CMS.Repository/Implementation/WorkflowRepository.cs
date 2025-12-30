using CMS.Domain;
using CMS.Domain.Entities;
using CMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Repository.Implementation;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly ApplicationDbContext _context;

    public WorkflowRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowStage> GetStageByIdAsync(int stageId)
    {
        return await _context.WorkflowStages
            .Include(x => x.WorkflowConfigurations)
            .FirstOrDefaultAsync(x => x.Id == stageId);
    }

    public async Task<WorkflowStage> GetFirstStageAsync()
    {
        return await _context.WorkflowStages
            .Include(x => x.WorkflowConfigurations)
            .OrderBy(x => x.StageOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<List<WorkflowConfiguration>> GetConfigurationsByStageIdAsync(int stageId, int? positionId = null, int? trackId = null)
    {
        var query = _context.WorkflowConfigurations
            .Include(x => x.WorkflowStage)
            .Include(x => x.NextStage)
            .Where(x => x.WorkflowStageId == stageId);

        // Filter by position if specified
        if (positionId.HasValue)
        {
            query = query.Where(x => x.PositionId == null || x.PositionId == positionId.Value);
        }

        // Filter by track if specified
        if (trackId.HasValue)
        {
            query = query.Where(x => x.TrackId == null || x.TrackId == trackId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<WorkflowConfiguration> GetConfigurationByStageAndRoleAsync(int stageId, string roleName, int? positionId = null, int? trackId = null)
    {
        var query = _context.WorkflowConfigurations
            .Include(x => x.WorkflowStage)
            .Include(x => x.NextStage)
            .Where(x => x.WorkflowStageId == stageId && x.RoleName == roleName);

        // Prefer position/track specific, then fallback to general
        if (positionId.HasValue)
        {
            query = query.OrderByDescending(x => x.PositionId == positionId.Value);
        }

        if (trackId.HasValue)
        {
            query = query.OrderByDescending(x => x.TrackId == trackId.Value);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<WorkflowStage>> GetAllStagesAsync()
    {
        return await _context.WorkflowStages
            .Include(x => x.WorkflowConfigurations)
            .OrderBy(x => x.StageOrder)
            .ToListAsync();
    }

    public async Task<List<WorkflowConfiguration>> GetAllConfigurationsAsync()
    {
        return await _context.WorkflowConfigurations
            .Include(x => x.WorkflowStage)
            .Include(x => x.NextStage)
            .Include(x => x.Position)
            .Include(x => x.Track)
            .ToListAsync();
    }

    public async Task<WorkflowStage> CreateStageAsync(WorkflowStage stage)
    {
        _context.WorkflowStages.Add(stage);
        await _context.SaveChangesAsync();
        return stage;
    }

    public async Task<WorkflowConfiguration> CreateConfigurationAsync(WorkflowConfiguration configuration)
    {
        _context.WorkflowConfigurations.Add(configuration);
        await _context.SaveChangesAsync();
        return configuration;
    }

    public async Task<WorkflowStage> UpdateStageAsync(WorkflowStage stage)
    {
        _context.WorkflowStages.Update(stage);
        await _context.SaveChangesAsync();
        return stage;
    }

    public async Task<WorkflowConfiguration> UpdateConfigurationAsync(WorkflowConfiguration configuration)
    {
        _context.WorkflowConfigurations.Update(configuration);
        await _context.SaveChangesAsync();
        return configuration;
    }

    public async Task<bool> DeleteStageAsync(int stageId)
    {
        var stage = await _context.WorkflowStages.FindAsync(stageId);
        if (stage == null) return false;

        _context.WorkflowStages.Remove(stage);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteConfigurationAsync(int configurationId)
    {
        var config = await _context.WorkflowConfigurations.FindAsync(configurationId);
        if (config == null) return false;

        _context.WorkflowConfigurations.Remove(config);
        await _context.SaveChangesAsync();
        return true;
    }
}

