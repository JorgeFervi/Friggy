using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Friggy.Infrastructure.Persistence;

public sealed class FriggyDbContextFactory : IDesignTimeDbContextFactory<FriggyDbContext>
{
    private const string LocalConnectionString =
        "Host=localhost;Port=5432;Database=friggy;Username=friggy;Password=friggy_local";

    public FriggyDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var connectionString = Environment.GetEnvironmentVariable("FRIGGY_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = LocalConnectionString;
        }

        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName))
            .Options;

        return new FriggyDbContext(options);
    }
}
