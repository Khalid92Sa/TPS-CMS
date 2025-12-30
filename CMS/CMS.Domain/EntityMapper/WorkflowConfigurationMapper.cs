using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class WorkflowConfigurationMapper : IEntityTypeConfiguration<WorkflowConfiguration>
{
    public void Configure(EntityTypeBuilder<WorkflowConfiguration> builder)
    {
        builder.ToTable("WorkflowConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SecondInterviewerRoleName)
            .HasMaxLength(100);

        builder.Property(x => x.RequiresSecondInterviewer)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CanStopWorkflow)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(x => x.WorkflowStage)
            .WithMany(x => x.WorkflowConfigurations)
            .HasForeignKey(x => x.WorkflowStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NextStage)
            .WithMany()
            .HasForeignKey(x => x.NextStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Track)
            .WithMany()
            .HasForeignKey(x => x.TrackId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for faster lookups
        builder.HasIndex(x => new { x.WorkflowStageId, x.RoleName });
        builder.HasIndex(x => new { x.PositionId, x.TrackId });
    }
}

