using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.DataSeeds;

internal class StatusSeed : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> builder)
    {
        builder.HasData(
                         new Status { Id = 1, Name = "Pending", Code = "PND" },
                         new Status { Id = 2, Name = "Approved", Code = "APR" },
                         new Status { Id = 3, Name = "Rejected", Code = "REJ" },
                         new Status { Id = 4, Name = "On hold", Code = "HOLD" }
                       );
    }
}