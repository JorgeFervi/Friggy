using Friggy.IntegrationTests.Testing;
using Microsoft.EntityFrameworkCore;

namespace Friggy.IntegrationTests.Persistence;

public sealed class PostgreSqlHarnessTests(PostgreSqlDatabaseFixture database)
    : DatabaseIntegrationTest(database), IClassFixture<PostgreSqlDatabaseFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task InitializeAsync_NewPhysicalDatabase_AppliesAllMigrations()
    {
        await using var context = Database.CreateDbContext();

        var appliedMigrations = await context.Database
            .GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Collection(
            appliedMigrations,
            migration => Assert.EndsWith("_InitialInfrastructure", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddCatalogs", migration, StringComparison.Ordinal),
            migration => Assert.EndsWith("_AddRecipes", migration, StringComparison.Ordinal));
        Assert.StartsWith("friggy_tests_", Database.DatabaseName, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResetAsync_StateCreatedByTest_RemovesStateAndReappliesMigrations()
    {
        await using (var context = Database.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE temporary_harness_state (id integer PRIMARY KEY)",
                TestContext.Current.CancellationToken);
        }

        await Database.ResetAsync(TestContext.Current.CancellationToken);

        await using var resetContext = Database.CreateDbContext();
        var connection = resetContext.Database.GetDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.temporary_harness_state') IS NULL";

        var tableWasRemoved = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);
        var appliedMigrations = await resetContext.Database
            .GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(true, tableWasRemoved);
        Assert.Equal(3, appliedMigrations.Count());
    }
}
