using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

internal class CandidateMapper : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder
            .HasOne(candidate => candidate.CV)
            .WithMany()
            .HasForeignKey(candidate => candidate.CVAttachmentId);
        builder
            .HasOne(candidate => candidate.Position)
            .WithMany(position => position.Candidates)
            .HasForeignKey(candidate => candidate.PositionId);
        builder
           .HasOne(candidate => candidate.Track)
           .WithMany(track => track.Candidates)
           .HasForeignKey(candidate => candidate.TrackId);

        builder
            .HasMany(candidate => candidate.Interviews)
            .WithOne(interview => interview.Candidate);

        builder.HasOne(candidate => candidate.Company)
            .WithMany(company => company.Candidates)
            .HasForeignKey(candidate => candidate.CompanyId);
    }
}
