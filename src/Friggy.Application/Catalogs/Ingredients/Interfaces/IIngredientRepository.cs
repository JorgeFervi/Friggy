using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.Ingredients.Interfaces;

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken);
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);
    void Remove(Ingredient ingredient);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
