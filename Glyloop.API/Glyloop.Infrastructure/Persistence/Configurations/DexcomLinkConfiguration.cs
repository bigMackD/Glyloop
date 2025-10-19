using Glyloop.Domain.Aggregates.DexcomLink;
using Glyloop.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glyloop.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for DexcomLink aggregate.
/// Maps domain aggregate to PostgreSQL table with proper column types and indexes.
/// </summary>
public class DexcomLinkConfiguration : IEntityTypeConfiguration<DexcomLink>
{
    public void Configure(EntityTypeBuilder<DexcomLink> builder)
    {
        builder.ToTable("DexcomLinks");

        // Primary key
        builder.HasKey(d => d.Id);

        // UserId - value object conversion with explicit column name
        builder.Property(d => d.UserId)
            .IsRequired()
            .HasConversion(ValueObjectConverters.UserIdConverter)
            .HasColumnName("UserId");

        // EncryptedAccessToken - binary storage
        builder.Property(d => d.EncryptedAccessToken)
            .IsRequired()
            .HasColumnType("bytea");

        // EncryptedRefreshToken - binary storage
        builder.Property(d => d.EncryptedRefreshToken)
            .IsRequired()
            .HasColumnType("bytea");

        // TokenExpiresAt - timestamptz
        builder.Property(d => d.TokenExpiresAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // LastRefreshedAt - timestamptz
        builder.Property(d => d.LastRefreshedAt)
            .IsRequired()
            .HasColumnType("timestamptz");

        // Ignore computed properties (not persisted)
        builder.Ignore(d => d.IsActive);
        builder.Ignore(d => d.ShouldRefresh);

        // Ignore domain events collection (handled by base Entity class)
        builder.Ignore(d => d.DomainEvents);

        // Index on UserId for efficient user-based queries
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("IX_DexcomLinks_UserId");

        // Optional: Unique constraint to enforce one active link per user
        // Uncomment if business rule enforces single link per user at DB level
        // builder.HasIndex(d => d.UserId).IsUnique();
    }
}

