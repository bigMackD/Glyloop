using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for Event aggregate (base class).
/// Uses Table-Per-Type (TPT) inheritance strategy.
/// Each derived type (FoodEvent, InsulinEvent, etc.) gets its own table.
/// </summary>
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        // Primary key
        builder.HasKey(e => e.Id);

        // TPT inheritance strategy
        builder.UseTptMappingStrategy();

        // UserId - value object conversion
        builder.Property(e => e.UserId)
            .IsRequired()
            .HasConversion(ValueObjectConverters.UserIdConverter);

        // EventTime - timestamptz
        builder.Property(e => e.EventTime)
            .IsRequired()
            .HasColumnType("timestamptz");

        // CreatedAt - timestamptz
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // EventType - discriminator (stored as int)
        builder.Property(e => e.EventType)
            .IsRequired();

        // Source - stored as int
        builder.Property(e => e.Source)
            .IsRequired();

        // Note - optional NoteText value object
        builder.Property(e => e.Note)
            .HasConversion(ValueObjectConverters.NullableNoteTextConverter)
            .HasMaxLength(500)
            .IsRequired(false);

        // Ignore domain events collection (handled by base Entity class)
        builder.Ignore(e => e.DomainEvents);

        // Composite index for efficient user and time-based queries
        builder.HasIndex(e => new { e.UserId, e.EventTime })
            .HasDatabaseName("IX_Events_UserId_EventTime");

        // Index on EventType for filtering by event type
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_Events_EventType");
    }
}

