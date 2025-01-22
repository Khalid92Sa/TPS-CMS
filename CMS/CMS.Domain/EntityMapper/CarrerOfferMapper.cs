using CMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Domain.EntityMapper;

internal class CarrerOfferMapper : IEntityTypeConfiguration<CarrerOffer>
{
    public void Configure(EntityTypeBuilder<CarrerOffer> builder)
    {
        builder
            .HasOne(o => o.Position)
            .WithMany(p => p.CarrerOffer)
            .HasForeignKey(o => o.PositionId);

        builder
            .HasOne(o => o.Creator)
            .WithMany()
            .HasForeignKey(o => o.CreatedBy);
    }
}
