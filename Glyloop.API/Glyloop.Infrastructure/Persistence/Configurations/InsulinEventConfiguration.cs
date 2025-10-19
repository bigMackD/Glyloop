using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for InsulinEvent.
/// TPT child table containing insulin-specific properties.
/// </summary>
public class InsulinEventConfiguration : IEntityTypeConfiguration<InsulinEvent>
{
    public void Configure(EntityTypeBuilder<InsulinEvent> builder)
    {
        builder.ToTable("InsulinEvents");

        // InsulinType - stored as int enum (Fast=0, Long=1)
        builder.Property(e => e.InsulinType)
            .IsRequired();

        // Dose - value object conversion to decimal
        builder.Property(e => e.Dose)
            .IsRequired()
            .HasConversion(ValueObjectConverters.InsulinDoseConverter)
            .HasColumnType("decimal(5,2)");

        // Preparation - optional string
        builder.Property(e => e.Preparation)
            .HasMaxLength(200)
            .IsRequired(false);

        // Delivery - optional string
        builder.Property(e => e.Delivery)
            .HasMaxLength(200)
            .IsRequired(false);

        // Timing - optional string
        builder.Property(e => e.Timing)
            .HasMaxLength(200)
            .IsRequired(false);
    }
}

