using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class InterviewDelegationMapper : IEntityTypeConfiguration<InterviewDelegation>
{
    public void Configure(EntityTypeBuilder<InterviewDelegation> builder)
    {
        builder.ToTable("InterviewDelegations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.OriginalInterviewerId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.DelegatedUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.DelegatedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.RemovedByUserId)
            .HasMaxLength(450);

        builder.Property(d => d.InterviewerSlot)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.DurationName)
            .HasMaxLength(100);

        builder.HasIndex(d => d.InterviewId);
        builder.HasIndex(d => d.CandidateId);
        builder.HasIndex(d => new { d.InterviewId, d.DelegatedUserId });
    }
}
