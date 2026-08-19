using Friggy.Application.Inventory.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core que consulta las referencias de catálogo necesarias para
/// el inventario.
/// </summary>
public sealed class InventoryReferenceRepository(FriggyDbContext context)
    : IInventoryReferenceRepository
{
    /// <summary>
    /// Obtiene los ingredientes disponibles sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<Ingredient>> ListIngredientsAsync(
        CancellationToken cancellationToken) =>
        await context.Ingredients.AsNoTracking().ToListAsync(cancellationToken);

    /// <summary>
    /// Obtiene las unidades de medida disponibles sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<UnitType>> ListUnitTypesAsync(
        CancellationToken cancellationToken) =>
        await context.UnitTypes.AsNoTracking().ToListAsync(cancellationToken);
}
