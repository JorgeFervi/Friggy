using Microsoft.Extensions.Options;

namespace Friggy.Web.Api;

public static class FriggyApiServiceCollectionExtensions
{
    public static IServiceCollection AddFriggyApiClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<FriggyApiOptions>()
            .Bind(configuration.GetSection(FriggyApiOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.BaseUrl is { IsAbsoluteUri: true } baseUrl &&
                    (baseUrl.Scheme == Uri.UriSchemeHttp || baseUrl.Scheme == Uri.UriSchemeHttps),
                $"{FriggyApiOptions.SectionName}:BaseUrl debe ser una URL HTTP o HTTPS absoluta.")
            .ValidateOnStart();

        services.AddHttpClient<IIngredientsApiClient, CatalogApiClient>(ConfigureClient);
        services.AddHttpClient<IUnitTypesApiClient, CatalogApiClient>(ConfigureClient);
        services.AddHttpClient<IRecipeTagsApiClient, CatalogApiClient>(ConfigureClient);
        services.AddHttpClient<IMealTypesApiClient, CatalogApiClient>(ConfigureClient);
        services.AddHttpClient<IRecipesApiClient, RecipeApiClient>(ConfigureClient);
        services.AddHttpClient<IDailyPlansApiClient, DailyPlanApiClient>(ConfigureClient);
        services.AddHttpClient<IDailyPlanTemplatesApiClient, DailyPlanTemplatesApiClient>(ConfigureClient);
        services.AddHttpClient<IInventoryApiClient, InventoryApiClient>(ConfigureClient);

        return services;
    }

    private static void ConfigureClient(IServiceProvider services, HttpClient client)
    {
        var options = services.GetRequiredService<IOptions<FriggyApiOptions>>().Value;
        client.BaseAddress = options.BaseUrl;
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }
}
