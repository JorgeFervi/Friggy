namespace Friggy.IntegrationTests.Testing;

public abstract class DatabaseIntegrationTest(PostgreSqlDatabaseFixture database) : IAsyncLifetime
{
    protected PostgreSqlDatabaseFixture Database { get; } = database;

    public async ValueTask InitializeAsync()
    {
        await Database.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
