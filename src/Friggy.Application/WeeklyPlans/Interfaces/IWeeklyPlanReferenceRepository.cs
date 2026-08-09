using Friggy.Domain.Catalogs;

namespace Friggy.Application.WeeklyPlans.Interfaces;

public interface IWeeklyPlanReferenceRepository
{
    Task<bool> RecipeExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> MealTypeExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MealType>> ListMealTypesAsync(CancellationToken cancellationToken);
}
