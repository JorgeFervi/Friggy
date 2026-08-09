using Friggy.Application.WeeklyPlans.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class WeeklyPlanReferenceRepository(FriggyDbContext context)
    : IWeeklyPlanReferenceRepository
{
    public Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Recipes.AnyAsync(recipe => recipe.Id == id, cancellationToken);

    public Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.MealTypes.AnyAsync(mealType => mealType.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MealType>> ListMealTypesAsync(
        CancellationToken cancellationToken) =>
        await context.MealTypes
            .AsNoTracking()
            .OrderBy(mealType => mealType.Order)
            .ThenBy(mealType => mealType.Name.Normalized)
            .ToListAsync(cancellationToken);
}
