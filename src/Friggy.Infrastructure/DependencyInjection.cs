using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Infrastructure.Persistence;
using Friggy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Friggy");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión 'ConnectionStrings:Friggy' es obligatoria.");
        }

        services.AddDbContext<FriggyDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsAssembly(typeof(FriggyDbContext).Assembly.FullName)));

        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IUnitTypeRepository, UnitTypeRepository>();
        services.AddScoped<IRecipeTagRepository, RecipeTagRepository>();
        services.AddScoped<IMealTypeRepository, MealTypeRepository>();

        return services;
    }
}
