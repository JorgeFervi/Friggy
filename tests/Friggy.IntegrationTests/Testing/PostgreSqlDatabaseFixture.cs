using Friggy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Friggy.IntegrationTests.Testing;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container;

    public PostgreSqlDatabaseFixture()
    {
        DatabaseName = $"friggy_tests_{Guid.NewGuid():N}";
        container = new PostgreSqlBuilder("postgres:17.6-alpine")
            .WithDatabase(DatabaseName)
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public string ConnectionString => container.GetConnectionString();

    public string DatabaseName { get; }

    public async ValueTask InitializeAsync()
    {
        await container.StartAsync(TestContext.Current.CancellationToken);
        await MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }

    public FriggyDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(
                ConnectionString,
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName))
            .Options;

        return new FriggyDbContext(options);
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using (var context = CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "DROP SCHEMA public CASCADE; CREATE SCHEMA public;",
                cancellationToken);
        }

        await MigrateAsync(cancellationToken);
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
