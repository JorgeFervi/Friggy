using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence;

public sealed class FriggyDbContext(DbContextOptions<FriggyDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FriggyDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
