using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace CMS.Domain.DataSeeds;

/// <summary>
/// Seeds the default interview workflow stages
/// This supports flexible workflows based on who starts the interview
/// </summary>
public class WorkflowSeed : IEntityTypeConfiguration<WorkflowStage>
{
    public void Configure(EntityTypeBuilder<WorkflowStage> builder)
    {
        builder.HasData(
            // Stage 1: Initial Interview (can be started by any role)
            new WorkflowStage
            {
                Id = 1,
                Name = "Initial Interview",
                StageOrder = 1,
                Description = "First interview - can be conducted by any role",
                IsFinalStage = false,
                IsParallelStage = false,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 2: Management Review (GM and/or Architecture)
            new WorkflowStage
            {
                Id = 2,
                Name = "Management Review",
                StageOrder = 2,
                Description = "Management and architecture review",
                IsFinalStage = false,
                IsParallelStage = true, // GM and Architecture can interview in parallel
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 3: Final HR Interview
            new WorkflowStage
            {
                Id = 3,
                Name = "Final HR Interview",
                StageOrder = 3,
                Description = "Final HR interview and decision",
                IsFinalStage = true, // This is the last stage
                IsParallelStage = false,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 4: HR Initial Interview (for reverse workflow)
            new WorkflowStage
            {
                Id = 4,
                Name = "HR Initial Interview",
                StageOrder = 1,
                Description = "HR interview - first stage in reverse workflow",
                IsFinalStage = false,
                IsParallelStage = false,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 5: Interviewers Stage (for reverse workflow)
            new WorkflowStage
            {
                Id = 5,
                Name = "Interviewers Review",
                StageOrder = 2,
                Description = "Interviewers review - second stage in reverse workflow",
                IsFinalStage = false,
                IsParallelStage = false,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 6: GM Final Stage (for reverse workflow)
            new WorkflowStage
            {
                Id = 6,
                Name = "GM Final Review",
                StageOrder = 3,
                Description = "GM final review - last stage in reverse workflow",
                IsFinalStage = true, // This is the last stage in reverse workflow
                IsParallelStage = false,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            }
        );
    }
}

/// <summary>
/// Seeds the workflow configurations
/// Defines what happens when each role approves at each stage
/// </summary>
public class WorkflowConfigurationSeed : IEntityTypeConfiguration<WorkflowConfiguration>
{
    public void Configure(EntityTypeBuilder<WorkflowConfiguration> builder)
    {
        builder.HasData(
            // ========== STAGE 1 CONFIGURATIONS ==========
            // When Interviewer completes Stage 1 → Create Stage 2 (GM + Architecture)
            new WorkflowConfiguration
            {
                Id = 1,
                WorkflowStageId = 1,
                RoleName = "Interviewer",
                NextStageId = 2, // Goes to Stage 2 (GM + Architecture)
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // When General Manager completes Stage 1 → Go directly to HR (Stage 3)
            // NOTE: If both GM and Architecture selected together, logic handles direct HR path
            new WorkflowConfiguration
            {
                Id = 2,
                WorkflowStageId = 1,
                RoleName = "General Manager",
                NextStageId = 3, // Goes directly to Stage 3 (HR) - unless both GM+Archi selected (handled in logic)
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // When Solution Architecture completes Stage 1 → Go to GM (Stage 2)
            // NOTE: If both GM and Architecture selected together, logic handles direct HR path
            new WorkflowConfiguration
            {
                Id = 3,
                WorkflowStageId = 1,
                RoleName = "Solution Architecture",
                NextStageId = 2, // Goes to Stage 2 (GM) - unless both GM+Archi selected (handled in logic)
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },

            // ========== STAGE 2 CONFIGURATIONS ==========
            // When General Manager completes Stage 2 → Go to HR (Stage 3)
            new WorkflowConfiguration
            {
                Id = 4,
                WorkflowStageId = 2,
                RoleName = "General Manager",
                NextStageId = 3, // Goes to Stage 3 (HR)
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // When Solution Architecture completes Stage 2 → Go to GM (Stage 2) or HR if GM already done
            // Note: This is handled in logic - if GM already approved, go to HR
            new WorkflowConfiguration
            {
                Id = 5,
                WorkflowStageId = 2,
                RoleName = "Solution Architecture",
                NextStageId = 2, // Goes to GM (Stage 2) - but logic checks if GM already done
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },

            // ========== STAGE 3 CONFIGURATIONS ==========
            // When HR Manager completes Stage 3 → END (no next stage)
            new WorkflowConfiguration
            {
                Id = 6,
                WorkflowStageId = 3,
                RoleName = "HR Manager",
                NextStageId = null, // No next stage - this is final
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },

            // ========== REVERSE WORKFLOW CONFIGURATIONS ==========
            // Stage 4: HR Initial Interview → Stage 5 (Interviewers)
            new WorkflowConfiguration
            {
                Id = 7,
                WorkflowStageId = 4,
                RoleName = "HR Manager",
                NextStageId = 5, // Goes to Interviewers Review
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 5: Interviewers Review → Stage 6 (GM Final)
            new WorkflowConfiguration
            {
                Id = 8,
                WorkflowStageId = 5,
                RoleName = "Interviewer",
                NextStageId = 6, // Goes to GM Final Review
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 5: If GM is one of the interviewers → Stage 6 (GM Final) or back to HR if GM was selected
            new WorkflowConfiguration
            {
                Id = 9,
                WorkflowStageId = 5,
                RoleName = "General Manager",
                NextStageId = 4, // Goes back to HR if GM was selected as interviewer
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            },
            // Stage 6: GM Final Review → END (no next stage)
            new WorkflowConfiguration
            {
                Id = 10,
                WorkflowStageId = 6,
                RoleName = "General Manager",
                NextStageId = null, // No next stage - this is final in reverse workflow
                RequiresSecondInterviewer = false,
                CanStopWorkflow = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "System",
                ModifiedOn = DateTime.Now,
                ModifiedBy = "System",
                IsActive = true,
                IsDelete = false
            }
        );
    }
}
