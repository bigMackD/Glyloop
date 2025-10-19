using Glyloop.Domain.Aggregates.DexcomLink;
using Glyloop.Domain.Aggregates.Event;
using Glyloop.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Glyloop.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for the Glyloop application.
/// Integrates with ASP.NET Core Identity using Guid keys.
/// 
/// Design:
/// - Inherits from IdentityDbContext with Guid for all Identity keys
/// - Manages DexcomLink and Event aggregates
/// - Uses PostgreSQL with Npgsql provider
/// - All timestamps stored as timestamptz (UTC with offset)
/// - Applies configurations via assembly scanning
/// </summary>
public class GlyloopDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public GlyloopDbContext(DbContextOptions<GlyloopDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// DbSet for DexcomLink aggregate root.
    /// </summary>
    public DbSet<DexcomLink> DexcomLinks => Set<DexcomLink>();

    /// <summary>
    /// DbSet for Event aggregate root (includes all polymorphic event types).
    /// EF Core handles Table-Per-Type (TPT) inheritance automatically.
    /// </summary>
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(GlyloopDbContext).Assembly);
    }

    /// <summary>
    /// Override SaveChangesAsync to handle domain event interception.
    /// Note: Domain event dispatching is handled by UnitOfWork, not here.
    /// This override is reserved for future cross-cutting concerns (audit, etc.)
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Future: Add audit timestamps, soft delete, etc. here
        return base.SaveChangesAsync(cancellationToken);
    }
}

