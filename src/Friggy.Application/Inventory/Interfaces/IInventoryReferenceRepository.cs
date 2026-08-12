using Friggy.Domain.Catalogs;

namespace Friggy.Application.Inventory.Interfaces;

public interface IInventoryReferenceRepository
{
    Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        CancellationToken cancellationToken);
}
