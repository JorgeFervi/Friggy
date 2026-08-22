using Friggy.Web.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Friggy.ComponentTests.Api;

public sealed class FriggyApiRegistrationTests
{
    [Theory]
    [InlineData(null, "30")]
    [InlineData("/api", "30")]
    [InlineData("ftp://localhost", "30")]
    [InlineData("http://localhost:5292", "0")]
    public async Task Start_InvalidConfiguration_ThrowsOptionsValidationException(
        string? baseUrl,
        string timeoutSeconds)
    {
        var configuration = new Dictionary<string, string?>
        {
            [$"{FriggyApiOptions.SectionName}:BaseUrl"] = baseUrl,
            [$"{FriggyApiOptions.SectionName}:TimeoutSeconds"] = timeoutSeconds,
        };
        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(configuration))
            .ConfigureServices((context, services) =>
                services.AddFriggyApiClients(context.Configuration))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(
            () => host.StartAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Start_ValidConfiguration_BindsOptionsAndRegistersClients()
    {
        var configuration = new Dictionary<string, string?>
        {
            [$"{FriggyApiOptions.SectionName}:BaseUrl"] = "https://api.friggy.test/root/",
            [$"{FriggyApiOptions.SectionName}:TimeoutSeconds"] = "17",
        };
        using var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(configuration))
            .ConfigureServices((context, services) =>
                services.AddFriggyApiClients(context.Configuration))
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var options = host.Services.GetRequiredService<IOptions<FriggyApiOptions>>().Value;
        Assert.Equal(new Uri("https://api.friggy.test/root/"), options.BaseUrl);
        Assert.Equal(17, options.TimeoutSeconds);
        Assert.IsType<CatalogApiClient>(host.Services.GetRequiredService<IIngredientsApiClient>());
        Assert.IsType<CatalogApiClient>(host.Services.GetRequiredService<IUnitTypesApiClient>());
        Assert.IsType<CatalogApiClient>(host.Services.GetRequiredService<IRecipeTagsApiClient>());
        Assert.IsType<CatalogApiClient>(host.Services.GetRequiredService<IMealTypesApiClient>());
        Assert.IsType<RecipeApiClient>(host.Services.GetRequiredService<IRecipesApiClient>());
        Assert.IsType<DailyPlanApiClient>(host.Services.GetRequiredService<IDailyPlansApiClient>());
    }
}
