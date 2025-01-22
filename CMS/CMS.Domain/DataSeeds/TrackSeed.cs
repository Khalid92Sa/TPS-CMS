using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.DataSeeds;

public class TrackSeed : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasData(
                         new Track { Id = 1, Name = ".NET" },
                         new Track { Id = 2, Name = "QC" },
                         new Track { Id = 3, Name = "BA" },
                         new Track { Id = 4, Name = "PM" },
                         new Track { Id = 5, Name = "IT" },
                         new Track { Id = 6, Name = "Frontend" },
                         new Track { Id = 7, Name = "UI/UX" }
                         //new Track { Id = 8, Name = "Finance" },
                         //new Track { Id = 9, Name = "BI" },
                         //new Track { Id = 10, Name = "SharePoint" }
                       );
    }
}