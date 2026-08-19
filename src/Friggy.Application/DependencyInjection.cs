using Friggy.Application.Catalogs.Ingredients.Services;
using Friggy.Application.Catalogs.MealTypes.Services;
using Friggy.Application.Catalogs.RecipeTags.Services;
using Friggy.Application.Catalogs.UnitTypes.Services;
using Friggy.Application.Inventory.Services;
using Friggy.Application.Recipes.Services;
using Friggy.Application.WeeklyPlans.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.Application;

/// <summary>
/// Clase que contiene el registro de servicios de la capa de aplicación.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Método que registra los servicios de aplicación y sus dependencias.
    /// </summary>
    /// <param name="services">
    /// Colección de servicios donde se registrarán las implementaciones.
    /// </param>
    /// <returns>
    /// La misma colección de servicios para continuar con la configuración.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la colección de servicios es nula.
    /// </exception>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IngredientService>();
        services.AddScoped<UnitTypeService>();
        services.AddScoped<RecipeTagService>();
        services.AddScoped<MealTypeService>();
        services.AddScoped<RecipeService>();
        services.AddScoped<WeeklyPlanService>();
        services.AddScoped<InventoryLotService>();
        services.AddScoped<WeeklyPlanInventoryService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
