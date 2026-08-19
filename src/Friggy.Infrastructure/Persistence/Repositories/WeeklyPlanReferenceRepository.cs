using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core que consulta las referencias necesarias para los planes
/// semanales.
/// </summary>
public sealed class WeeklyPlanReferenceRepository(FriggyDbContext context)
    : IWeeklyPlanReferenceRepository
{
    /// <summary>
    /// Comprueba que exista una receta.
    /// </summary>
    public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Recipes.AnyAsync(recipe => recipe.Id == id, cancellationToken);

    /// <summary>
    /// Obtiene el tiempo estimado de una receta.
    /// </summary>
    public Task<TimeSpan?> GetRecipeEstimatedTimeAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        context.Recipes
            .Where(recipe => recipe.Id == id)
            .Select(recipe => (TimeSpan?)recipe.EstimatedTime)
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Comprueba que exista un tipo de comida.
    /// </summary>
    public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.MealTypes.AnyAsync(mealType => mealType.Id == id, cancellationToken);

    /// <summary>
    /// Obtiene los tipos de comida ordenados para construir un plan semanal.
    /// </summary>
    public async Task<IReadOnlyList<MealType>> ListMealTypesAsync(
        CancellationToken cancellationToken) =>
        await context.MealTypes
            .AsNoTracking()
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Normalized)
            .ToListAsync(cancellationToken);
}
