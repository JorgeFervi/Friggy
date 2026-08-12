using Friggy.Application.Inventory.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class InventoryReferenceRepository(FriggyDbContext context)
    : IInventoryReferenceRepository
{
    public async Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(
        CancellationToken cancellationToken) =>
        await context.Ingredients.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        CancellationToken cancellationToken) =>
        await context.UnitTypes.AsNoTracking().ToListAsync(cancellationToken);
}
