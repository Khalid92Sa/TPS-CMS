using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class DelegationDurationMapper : IEntityTypeConfiguration<DelegationDuration>
{
    public void Configure(EntityTypeBuilder<DelegationDuration> builder)
    {
        builder.ToTable("DelegationDurations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Unit)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(d => d.Name);
    }
}
