using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class WorkflowStageMapper : IEntityTypeConfiguration<WorkflowStage>
{
    public void Configure(EntityTypeBuilder<WorkflowStage> builder)
    {
        builder.ToTable("WorkflowStages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.StageOrder)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsFinalStage)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsParallelStage)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasMany(x => x.WorkflowConfigurations)
            .WithOne(x => x.WorkflowStage)
            .HasForeignKey(x => x.WorkflowStageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

