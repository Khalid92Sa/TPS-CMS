using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

/// <summary>
/// Defines which roles can conduct interviews at a stage and what the next stage(s) should be
/// </summary>
public class WorkflowConfiguration : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public int WorkflowStageId { get; set; }

    public virtual WorkflowStage WorkflowStage { get; set; }

    [Required]
    public string RoleName { get; set; } // e.g., "Interviewer", "General Manager", "HR Manager"

    public int? NextStageId { get; set; } // Next stage to create when approved

    public virtual WorkflowStage NextStage { get; set; }

    public bool RequiresSecondInterviewer { get; set; } // Whether this stage requires a second interviewer

    public string? SecondInterviewerRoleName { get; set; } // Role required for second interviewer (if applicable)

    public bool CanStopWorkflow { get; set; } // Whether this role can stop the workflow (reject/on hold)

    public int? PositionId { get; set; } // Optional: Specific to a position

    public virtual Position Position { get; set; }

    public int? TrackId { get; set; } // Optional: Specific to a track

    public virtual Track Track { get; set; }
}

