namespace Friggy.EndToEndTests.Testing;

public sealed class FullStackFixture : IAsyncLifetime
{
    private const string WebBaseUrlVariable = "FRIGGY_WEB_BASE_URL";
    private readonly IFullStackRuntime runtime;
    private readonly IEnvironmentVariables environmentVariables;
    private string? previousWebBaseUrl;
    private bool initialized;

    public FullStackFixture()
        : this(new FullStackRuntime(), new SystemEnvironmentVariables())
    {
    }

    internal FullStackFixture(
        IFullStackRuntime runtime,
        IEnvironmentVariables environmentVariables)
    {
        this.runtime = runtime;
        this.environmentVariables = environmentVariables;
    }

    public Uri ApiBaseUrl => runtime.ApiBaseUrl;

    public Uri WebBaseUrl => runtime.WebBaseUrl;

    public async ValueTask InitializeAsync()
    {
        previousWebBaseUrl = environmentVariables.Get(WebBaseUrlVariable);

        try
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            await runtime.StartDatabaseAsync(cancellationToken);
            await runtime.MigrateAsync(cancellationToken);
            await StartServicesAsync(cancellationToken);
            environmentVariables.Set(WebBaseUrlVariable, WebBaseUrl.AbsoluteUri);
            initialized = true;
        }
        catch
        {
            await StopEnvironmentAsync(CancellationToken.None);
            environmentVariables.Set(WebBaseUrlVariable, previousWebBaseUrl);
            runtime.Dispose();
            throw;
        }
    }

    public async Task RestartServicesAsync(CancellationToken cancellationToken)
    {
        EnsureInitialized("reiniciarlo");

        await runtime.StopWebAsync(cancellationToken);
        await runtime.StopApiAsync(cancellationToken);
        await StartServicesAsync(cancellationToken);
    }

    public async Task ResetRecipeDataAsync(CancellationToken cancellationToken)
    {
        EnsureInitialized("reiniciar los datos de recetas");
        await runtime.ResetRecipeDataAsync(cancellationToken);
    }

    public async Task StopApiAsync(CancellationToken cancellationToken)
    {
        EnsureInitialized("detener la API");
        await runtime.StopApiAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await StopEnvironmentAsync(CancellationToken.None);
        environmentVariables.Set(WebBaseUrlVariable, previousWebBaseUrl);
        runtime.Dispose();
        initialized = false;
    }

    private async Task StartServicesAsync(CancellationToken cancellationToken)
    {
        await runtime.StartApiAsync(cancellationToken);
        await runtime.WaitForApiAsync(cancellationToken);
        await runtime.StartWebAsync(cancellationToken);
        await runtime.WaitForWebAsync(cancellationToken);
    }

    private async Task StopEnvironmentAsync(CancellationToken cancellationToken)
    {
        await runtime.StopWebAsync(cancellationToken);
        await runtime.StopApiAsync(cancellationToken);
        await runtime.StopDatabaseAsync(cancellationToken);
    }

    private void EnsureInitialized(string operation)
    {
        if (!initialized)
        {
            throw new InvalidOperationException(
                $"El entorno full-stack debe iniciarse antes de {operation}.");
        }
    }
}

internal interface IFullStackRuntime : IDisposable
{
    Uri ApiBaseUrl { get; }

    Uri WebBaseUrl { get; }

    Task StartDatabaseAsync(CancellationToken cancellationToken);

    Task MigrateAsync(CancellationToken cancellationToken);

    Task ResetRecipeDataAsync(CancellationToken cancellationToken);

    Task StartApiAsync(CancellationToken cancellationToken);

    Task WaitForApiAsync(CancellationToken cancellationToken);

    Task StartWebAsync(CancellationToken cancellationToken);

    Task WaitForWebAsync(CancellationToken cancellationToken);

    Task StopWebAsync(CancellationToken cancellationToken);

    Task StopApiAsync(CancellationToken cancellationToken);

    Task StopDatabaseAsync(CancellationToken cancellationToken);
}

internal interface IEnvironmentVariables
{
    string? Get(string name);

    void Set(string name, string? value);
}

internal sealed class SystemEnvironmentVariables : IEnvironmentVariables
{
    public string? Get(string name) => Environment.GetEnvironmentVariable(name);

    public void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);
}

public static class FullStackTestGroup
{
    public const string Name = "Friggy full-stack";
}

[CollectionDefinition(FullStackTestGroup.Name)]
public sealed class FullStackCollectionDefinition : ICollectionFixture<FullStackFixture>;
