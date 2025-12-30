using System.Collections.Generic;

namespace CMS.Application.DTOs;

public class WorkflowStageDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int StageOrder { get; set; }
    public string? Description { get; set; }
    public bool IsFinalStage { get; set; }
    public bool IsParallelStage { get; set; }
    public List<WorkflowConfigurationDTO> Configurations { get; set; }
}

public class WorkflowConfigurationDTO
{
    public int Id { get; set; }
    public int WorkflowStageId { get; set; }
    public string StageName { get; set; }
    public string RoleName { get; set; }
    public int? NextStageId { get; set; }
    public string? NextStageName { get; set; }
    public bool RequiresSecondInterviewer { get; set; }
    public string? SecondInterviewerRoleName { get; set; }
    public bool CanStopWorkflow { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public int? TrackId { get; set; }
    public string? TrackName { get; set; }
}

public class NextStageInfoDTO
{
    public int StageId { get; set; }
    public string StageName { get; set; }
    public List<string> RequiredRoles { get; set; }
    public bool IsParallelStage { get; set; }
    public bool RequiresSecondInterviewer { get; set; }
    public string? SecondInterviewerRoleName { get; set; }
}

