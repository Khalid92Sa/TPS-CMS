using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class SelectedInterviewersMapper : IEntityTypeConfiguration<SelectedInterviewers>
{
    public void Configure(EntityTypeBuilder<SelectedInterviewers> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Interview)
            .WithMany()
            .HasForeignKey(s => s.InterviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.FirstInterviewerId)
            .HasMaxLength(450);

        builder.Property(s => s.SecondInterviewerId)
            .HasMaxLength(450);

        builder.Property(s => s.ArchitectureInterviewerId)
            .HasMaxLength(450);
    }
}

