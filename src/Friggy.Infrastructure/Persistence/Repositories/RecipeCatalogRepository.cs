using Friggy.Application.Recipes.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class RecipeCatalogRepository(FriggyDbContext context)
    : IRecipeCatalogRepository
{
    public Task<bool> IngredientsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.Ingredients.Select(item => item.Id),
            ids,
            cancellationToken);

    public Task<bool> UnitTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.UnitTypes.Select(item => item.Id),
            ids,
            cancellationToken);

    public Task<bool> TagsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.RecipeTags.Select(item => item.Id),
            ids,
            cancellationToken);

    public Task<bool> MealTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.MealTypes.Select(item => item.Id),
            ids,
            cancellationToken);

    private static async Task<bool> AllExistAsync(
        IQueryable<Guid> availableIds,
        IReadOnlyCollection<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = requestedIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return true;
        }

        var existingCount = await availableIds.CountAsync(
            id => distinctIds.Contains(id),
            cancellationToken);
        return existingCount == distinctIds.Length;
    }
}
