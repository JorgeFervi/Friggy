using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.UnitTypes.Interfaces;

public interface IUnitTypeRepository
{
    Task<IReadOnlyList<UnitType>> ListAsync(CancellationToken cancellationToken);
    Task<UnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    Task AddAsync(UnitType item, CancellationToken cancellationToken);
    void Remove(UnitType item);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
