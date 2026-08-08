using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.RecipeTags.Interfaces;

public interface IRecipeTagRepository
{
    Task<IReadOnlyList<RecipeTag>> ListAsync(CancellationToken cancellationToken);
    Task<RecipeTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(RecipeTag item, CancellationToken cancellationToken);
    void Remove(RecipeTag item);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
