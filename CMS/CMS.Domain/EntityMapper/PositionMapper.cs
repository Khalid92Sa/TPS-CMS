using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

internal class PositionMapper : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.HasMany(p => p.Candidates).WithOne(p => p.Position);
        builder.HasMany(p => p.Interviews).WithOne(p => p.Position);
        builder.HasMany(p => p.CarrerOffer).WithOne(p => p.Position);
        builder.HasOne(p => p.Evaluation).WithMany().HasForeignKey(p => p.EvaluationId);
    }
}
