using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for NoteEvent.
/// TPT child table containing note-specific properties.
/// </summary>
public class NoteEventConfiguration : IEntityTypeConfiguration<NoteEvent>
{
    public void Configure(EntityTypeBuilder<NoteEvent> builder)
    {
        builder.ToTable("NoteEvents");

        // Text - value object conversion to string
        builder.Property(e => e.Text)
            .IsRequired()
            .HasConversion(ValueObjectConverters.NoteTextConverter)
            .HasMaxLength(500);
    }
}

