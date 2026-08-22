using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Application.DailyPlanTemplates.Interfaces;
using Friggy.Application.Inventory.Interfaces;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Application.ShoppingLists.Interfaces;
using Friggy.Infrastructure.Persistence;
using Friggy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.Infrastructure;

/// <summary>
/// Clase que contiene el registro de persistencia y servicios de infraestructura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Método que registra Entity Framework Core, PostgreSQL y los adaptadores
    /// de persistencia de la aplicación.
    /// </summary>
    /// <param name="services">
    /// Colección de servicios donde se registrarán las implementaciones.
    /// </param>
    /// <param name="configuration">
    /// Configuración que contiene la cadena de conexión de Friggy.
    /// </param>
    /// <returns>
    /// La misma colección de servicios para continuar con la configuración.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la colección de servicios o la configuración
    /// son nulas.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Excepción lanzada cuando no se ha configurado la cadena de conexión.
    /// </exception>
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
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeCatalogRepository, RecipeCatalogRepository>();
        services.AddScoped<IDailyPlanRepository, DailyPlanRepository>();
        services.AddScoped<IDailyPlanTemplateRepository, DailyPlanTemplateRepository>();
        services.AddScoped<IDailyPlanReferenceRepository, DailyPlanReferenceRepository>();
        services.AddScoped<IInventoryLotRepository, InventoryLotRepository>();
        services.AddScoped<IInventoryReferenceRepository, InventoryReferenceRepository>();
        services.AddScoped<IInventoryUnitOfWork, InventoryUnitOfWork>();
        services.AddScoped<IShoppingListReadRepository, ShoppingListReadRepository>();

        return services;
    }
}
