using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for ExerciseEvent.
/// TPT child table containing exercise-specific properties.
/// </summary>
public class ExerciseEventConfiguration : IEntityTypeConfiguration<ExerciseEvent>
{
    public void Configure(EntityTypeBuilder<ExerciseEvent> builder)
    {
        builder.ToTable("ExerciseEvents");

        // ExerciseType - value object conversion (int reference)
        builder.Property(e => e.ExerciseType)
            .IsRequired()
            .HasConversion(ValueObjectConverters.ExerciseTypeIdConverter);

        // Duration - value object conversion to int (minutes)
        builder.Property(e => e.Duration)
            .IsRequired()
            .HasConversion(ValueObjectConverters.ExerciseDurationConverter);

        // Intensity - stored as int enum
        builder.Property(e => e.Intensity)
            .IsRequired();
    }
}

