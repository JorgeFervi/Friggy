using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Friggy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Friggy.EndToEndTests.Testing;

internal sealed class FullStackRuntime : IFullStackRuntime
{
    private static readonly TimeSpan EndpointTimeout = TimeSpan.FromSeconds(60);
    private readonly PostgreSqlContainer database;
    private readonly HttpClient healthClient = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly string repositoryRoot;
    private readonly string logDirectory;
    private ManagedProcess? apiProcess;
    private ManagedProcess? webProcess;
    private bool databaseStopped;
    private bool disposed;

    public FullStackRuntime()
    {
        repositoryRoot = FindRepositoryRoot();
        logDirectory = Path.Combine(
            repositoryRoot,
            ".friggy",
            "e2e",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(logDirectory);

        database = new PostgreSqlBuilder("postgres:17.6-alpine")
            .WithDatabase($"friggy_e2e_{Guid.NewGuid():N}")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        ApiBaseUrl = CreateLoopbackUrl();
        WebBaseUrl = CreateLoopbackUrl();
    }

    public Uri ApiBaseUrl { get; }

    public Uri WebBaseUrl { get; }

    public Task StartDatabaseAsync(CancellationToken cancellationToken) =>
        database.StartAsync(cancellationToken);

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(
                database.GetConnectionString(),
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName))
            .Options;

        await using var context = new FriggyDbContext(options);
        await context.Database.MigrateAsync(cancellationToken);
    }

    public async Task ResetRecipeDataAsync(CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .Options;

        await using var context = new FriggyDbContext(options);
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE recipes CASCADE;",
            cancellationToken);
    }

    public async Task ResetPlanningDataAsync(CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .Options;

        await using var context = new FriggyDbContext(options);
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE weekly_plans CASCADE;",
            cancellationToken);
    }

    public async Task ResetInventoryDataAsync(CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<FriggyDbContext>()
            .UseNpgsql(database.GetConnectionString())
            .Options;

        await using var context = new FriggyDbContext(options);
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE inventory_lots CASCADE;",
            cancellationToken);
    }

    public Task StartApiAsync(CancellationToken cancellationToken)
    {
        apiProcess = StartProcess(
            "api",
            "src/Friggy.Api/bin/" + BuildConfiguration + "/net10.0/Friggy.Api.dll",
            new Dictionary<string, string>
            {
                ["ASPNETCORE_CONTENTROOT"] = Path.Combine(repositoryRoot, "src", "Friggy.Api"),
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = ApiBaseUrl.AbsoluteUri,
                ["ConnectionStrings__Friggy"] = database.GetConnectionString(),
            });
        return Task.CompletedTask;
    }

    public Task WaitForApiAsync(CancellationToken cancellationToken) =>
        WaitForEndpointAsync(new Uri(ApiBaseUrl, "health"), apiProcess, cancellationToken);

    public Task StartWebAsync(CancellationToken cancellationToken)
    {
        webProcess = StartProcess(
            "web",
            "src/Friggy.Web/bin/" + BuildConfiguration + "/net10.0/Friggy.Web.dll",
            new Dictionary<string, string>
            {
                ["ASPNETCORE_CONTENTROOT"] = Path.Combine(repositoryRoot, "src", "Friggy.Web"),
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = WebBaseUrl.AbsoluteUri,
                ["FriggyApi__BaseUrl"] = ApiBaseUrl.AbsoluteUri,
                ["FriggyApi__TimeoutSeconds"] = "30",
            });
        return Task.CompletedTask;
    }

    public Task WaitForWebAsync(CancellationToken cancellationToken) =>
        WaitForEndpointAsync(new Uri(WebBaseUrl, "health"), webProcess, cancellationToken);

    public async Task StopWebAsync(CancellationToken cancellationToken)
    {
        if (webProcess is not null)
        {
            await webProcess.StopAsync(cancellationToken);
            webProcess = null;
        }
    }

    public async Task StopApiAsync(CancellationToken cancellationToken)
    {
        if (apiProcess is not null)
        {
            await apiProcess.StopAsync(cancellationToken);
            apiProcess = null;
        }
    }

    public async Task StopDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!databaseStopped)
        {
            await database.DisposeAsync();
            databaseStopped = true;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            healthClient.Dispose();
            disposed = true;
        }
    }

    private ManagedProcess StartProcess(
        string name,
        string assemblyPath,
        IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, assemblyPath));

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        return ManagedProcess.Start(
            startInfo,
            Path.Combine(logDirectory, $"{name}.out.log"),
            Path.Combine(logDirectory, $"{name}.err.log"));
    }

    private async Task WaitForEndpointAsync(
        Uri endpoint,
        ManagedProcess? process,
        CancellationToken cancellationToken)
    {
        if (process is null)
        {
            throw new InvalidOperationException($"No se ha iniciado el proceso para '{endpoint}'.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(EndpointTimeout);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

        while (true)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"El proceso terminó antes de que '{endpoint}' estuviera disponible. " +
                    $"Revisa '{process.StandardOutputPath}' y '{process.StandardErrorPath}'.");
            }

            try
            {
                using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
                requestTimeout.CancelAfter(TimeSpan.FromSeconds(2));
                using var response = await healthClient.GetAsync(endpoint, requestTimeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException) when (!timeout.IsCancellationRequested)
            {
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"'{endpoint}' no estuvo disponible en {EndpointTimeout.TotalSeconds:N0} segundos. " +
                    $"Revisa '{process.StandardOutputPath}' y '{process.StandardErrorPath}'.");
            }

            try
            {
                await timer.WaitForNextTickAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"'{endpoint}' no estuvo disponible en {EndpointTimeout.TotalSeconds:N0} segundos. " +
                    $"Revisa '{process.StandardOutputPath}' y '{process.StandardErrorPath}'.");
            }
        }
    }

    private static Uri CreateLoopbackUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return new Uri($"http://127.0.0.1:{port}/");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Friggy.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException(
            "No se encontró la raíz del repositorio desde el directorio de ejecución E2E.");
    }

    private static string BuildConfiguration
    {
        get
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }
    }
}

internal sealed class ManagedProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly Stream standardOutput;
    private readonly Stream standardError;
    private readonly Task copyStandardOutput;
    private readonly Task copyStandardError;

    private ManagedProcess(
        Process process,
        Stream standardOutput,
        Stream standardError,
        string standardOutputPath,
        string standardErrorPath)
    {
        this.process = process;
        this.standardOutput = standardOutput;
        this.standardError = standardError;
        StandardOutputPath = standardOutputPath;
        StandardErrorPath = standardErrorPath;
        copyStandardOutput = process.StandardOutput.BaseStream.CopyToAsync(standardOutput);
        copyStandardError = process.StandardError.BaseStream.CopyToAsync(standardError);
    }

    public bool HasExited => process.HasExited;

    public string StandardOutputPath { get; }

    public string StandardErrorPath { get; }

    public static ManagedProcess Start(
        ProcessStartInfo startInfo,
        string standardOutputPath,
        string standardErrorPath)
    {
        var standardOutput = File.Create(standardOutputPath);
        var standardError = File.Create(standardErrorPath);
        try
        {
            var process = Process.Start(startInfo) ?? throw new InvalidOperationException(
                $"No se pudo iniciar '{startInfo.FileName}'.");
            return new ManagedProcess(
                process,
                standardOutput,
                standardError,
                standardOutputPath,
                standardErrorPath);
        }
        catch
        {
            standardOutput.Dispose();
            standardError.Dispose();
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync(cancellationToken);
        await copyStandardOutput;
        await copyStandardError;
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await standardOutput.DisposeAsync();
        await standardError.DisposeAsync();
        process.Dispose();
    }
}
