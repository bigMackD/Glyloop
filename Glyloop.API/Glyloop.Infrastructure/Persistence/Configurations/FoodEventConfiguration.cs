using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for FoodEvent.
/// TPT child table containing food-specific properties.
/// </summary>
public class FoodEventConfiguration : IEntityTypeConfiguration<FoodEvent>
{
    public void Configure(EntityTypeBuilder<FoodEvent> builder)
    {
        builder.ToTable("FoodEvents");

        // Carbohydrates - value object conversion
        builder.Property(e => e.Carbohydrates)
            .IsRequired()
            .HasConversion(ValueObjectConverters.CarbohydrateConverter);

        // MealTag - value object conversion (int reference)
        builder.Property(e => e.MealTag)
            .IsRequired()
            .HasConversion(ValueObjectConverters.MealTagIdConverter);

        // AbsorptionHint - stored as int enum
        builder.Property(e => e.AbsorptionHint)
            .IsRequired();
    }
}

