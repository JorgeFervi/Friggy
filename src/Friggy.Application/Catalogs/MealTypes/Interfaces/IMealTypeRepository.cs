using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.MealTypes.Interfaces;

public interface IMealTypeRepository
{
    Task<IReadOnlyList<MealType>> ListAsync(CancellationToken cancellationToken);
    Task<MealType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(MealType item, CancellationToken cancellationToken);
    void Remove(MealType item);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
