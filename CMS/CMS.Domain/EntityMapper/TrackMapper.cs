using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class TrackMapper : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasMany(t => t.Candidates).WithOne(t => t.Track);
        builder.HasMany(t => t.Interviews).WithOne(t => t.Track);
    }
}
