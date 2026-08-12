using Friggy.Application.Catalogs.Ingredients.Services;
using Friggy.Application.Catalogs.MealTypes.Services;
using Friggy.Application.Catalogs.RecipeTags.Services;
using Friggy.Application.Catalogs.UnitTypes.Services;
using Friggy.Application.Inventory.Services;
using Friggy.Application.Recipes.Services;
using Friggy.Application.WeeklyPlans.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.Application;

public static class DependencyInjection
{
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
