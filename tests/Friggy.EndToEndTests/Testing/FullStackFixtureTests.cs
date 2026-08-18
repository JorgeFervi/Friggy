namespace Friggy.EndToEndTests.Testing;

public sealed class FullStackFixtureTests
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task InitializeAsync_NewEnvironment_StartsDependenciesInObservableOrder()
    {
        var runtime = new RecordingFullStackRuntime();
        var environment = new RecordingEnvironmentVariables();
        var fixture = new FullStackFixture(runtime, environment);

        await fixture.InitializeAsync();

        Assert.Equal(
            ["start-database", "migrate", "start-api", "wait-api", "start-web", "wait-web"],
            runtime.Operations);
        Assert.Equal(runtime.WebBaseUrl.AbsoluteUri, environment["FRIGGY_WEB_BASE_URL"]);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task RestartServicesAsync_RunningEnvironment_RestartsProcessesWithoutReplacingDatabase()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.RestartServicesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["stop-web", "stop-api", "start-api", "wait-api", "start-web", "wait-web"],
            runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task StopApiAsync_RunningEnvironment_StopsOnlyApiProcess()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.StopApiAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["stop-api"], runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task ResetRecipeDataAsync_RunningEnvironment_DelegatesToIsolatedDatabase()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.ResetRecipeDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["reset-recipe-data"], runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task ResetPlanningDataAsync_RunningEnvironment_DelegatesToIsolatedDatabase()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.ResetPlanningDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["reset-planning-data"], runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task ResetInventoryDataAsync_RunningEnvironment_DelegatesToIsolatedDatabase()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.ResetInventoryDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["reset-inventory-data"], runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task ResetRecipeTagDataAsync_RunningEnvironment_DelegatesToIsolatedDatabase()
    {
        var runtime = new RecordingFullStackRuntime();
        var fixture = new FullStackFixture(runtime, new RecordingEnvironmentVariables());
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.ResetRecipeTagDataAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["reset-recipe-tag-data"], runtime.Operations);

        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DisposeAsync_InitializedEnvironment_StopsProcessesAndRestoresRunnerVariable()
    {
        var runtime = new RecordingFullStackRuntime();
        var environment = new RecordingEnvironmentVariables
        {
            ["FRIGGY_WEB_BASE_URL"] = "http://previous.example/",
        };
        var fixture = new FullStackFixture(runtime, environment);
        await fixture.InitializeAsync();
        runtime.Operations.Clear();

        await fixture.DisposeAsync();

        Assert.Equal(["stop-web", "stop-api", "stop-database"], runtime.Operations);
        Assert.Equal("http://previous.example/", environment["FRIGGY_WEB_BASE_URL"]);
    }

    private sealed class RecordingFullStackRuntime : IFullStackRuntime
    {
        public Uri ApiBaseUrl { get; } = new("http://127.0.0.1:5101/");

        public Uri WebBaseUrl { get; } = new("http://127.0.0.1:5102/");

        public List<string> Operations { get; } = [];

        public Task StartDatabaseAsync(CancellationToken cancellationToken) =>
            RecordAsync("start-database");

        public Task MigrateAsync(CancellationToken cancellationToken) => RecordAsync("migrate");

        public Task ResetRecipeDataAsync(CancellationToken cancellationToken) =>
            RecordAsync("reset-recipe-data");

        public Task ResetPlanningDataAsync(CancellationToken cancellationToken) =>
            RecordAsync("reset-planning-data");

        public Task ResetInventoryDataAsync(CancellationToken cancellationToken) =>
            RecordAsync("reset-inventory-data");

        public Task ResetRecipeTagDataAsync(CancellationToken cancellationToken) =>
            RecordAsync("reset-recipe-tag-data");

        public Task StartApiAsync(CancellationToken cancellationToken) => RecordAsync("start-api");

        public Task WaitForApiAsync(CancellationToken cancellationToken) => RecordAsync("wait-api");

        public Task StartWebAsync(CancellationToken cancellationToken) => RecordAsync("start-web");

        public Task WaitForWebAsync(CancellationToken cancellationToken) => RecordAsync("wait-web");

        public Task StopWebAsync(CancellationToken cancellationToken) => RecordAsync("stop-web");

        public Task StopApiAsync(CancellationToken cancellationToken) => RecordAsync("stop-api");

        public Task StopDatabaseAsync(CancellationToken cancellationToken) =>
            RecordAsync("stop-database");

        public void Dispose()
        {
        }

        private Task RecordAsync(string operation)
        {
            Operations.Add(operation);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEnvironmentVariables : IEnvironmentVariables
    {
        private readonly Dictionary<string, string?> values = new(StringComparer.Ordinal);

        public string? this[string name]
        {
            get => Get(name);
            set => Set(name, value);
        }

        public string? Get(string name) => values.GetValueOrDefault(name);

        public void Set(string name, string? value) => values[name] = value;
    }
}
