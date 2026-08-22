using Friggy.Application.DailyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>Consultas EF Core de referencias para planes diarios.</summary>
public sealed class DailyPlanReferenceRepository(FriggyDbContext context)
    : IDailyPlanReferenceRepository
{
    /// <inheritdoc />
    public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Recipes.AnyAsync(recipe => recipe.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, TimeSpan?>> GetRecipeEstimatedTimesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await context.Recipes
            .AsNoTracking()
            .Where(recipe => ids.Contains(recipe.Id))
            .ToDictionaryAsync(
                recipe => recipe.Id,
                recipe => (TimeSpan?)recipe.EstimatedTime,
                cancellationToken);

    /// <inheritdoc />
    public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.MealTypes.AnyAsync(mealType => mealType.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MealType>> ListMealTypesAsync(
        CancellationToken cancellationToken) =>
        await context.MealTypes
            .AsNoTracking()
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Normalized)
            .ToListAsync(cancellationToken);
}
