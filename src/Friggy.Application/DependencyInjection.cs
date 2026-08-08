using Friggy.Application.Catalogs.Ingredients.Services;
using Friggy.Application.Catalogs.MealTypes.Services;
using Friggy.Application.Catalogs.RecipeTags.Services;
using Friggy.Application.Catalogs.UnitTypes.Services;
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

        return services;
    }
}
