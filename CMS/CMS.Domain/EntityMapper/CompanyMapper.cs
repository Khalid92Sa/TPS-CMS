using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

public class CompanyMapper : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder
            .HasOne(p => p.Country)
            .WithMany(p => p.Companies)
            .HasForeignKey(p => p.CountryId);

        builder.HasMany(p => p.Candidates)
            .WithOne(p => p.Company);
    }
}
