using Friggy.Application.Recipes.Dtos;
using Friggy.Domain.Recipes;

namespace Friggy.Application.Recipes.Interfaces;

public interface IRecipeRepository
{
    Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeListItemResponse>> ListSummariesAsync(
        CancellationToken cancellationToken);

    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);

    void Remove(Recipe recipe);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
