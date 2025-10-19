using Glyloop.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for ApplicationUser.
/// Configures custom properties beyond the default Identity schema.
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // CreatedAt timestamp with default value
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("NOW()");

        // LastLoginAt timestamp (nullable)
        builder.Property(u => u.LastLoginAt)
            .HasColumnType("timestamptz");

        // Note: Navigation properties to DexcomLinks and Events are not configured here
        // because UserId in those aggregates is a value object, not a direct Guid.
        // Foreign key relationships are established through the UserId Guid column.
    }
}

