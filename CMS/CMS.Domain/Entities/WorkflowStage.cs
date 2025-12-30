using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMS.Domain.Entities;

/// <summary>
/// Represents a stage in the interview workflow (e.g., "First Interview", "Second Interview", "Final Interview")
/// </summary>
public class WorkflowStage : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public int StageOrder { get; set; } // Order in the workflow (1, 2, 3, etc.)

    public string? Description { get; set; }

    public bool IsFinalStage { get; set; } // True if this is the last stage

    public bool IsParallelStage { get; set; } // True if multiple interviews can run in parallel at this stage

    public virtual ICollection<WorkflowConfiguration> WorkflowConfigurations { get; set; }
}

