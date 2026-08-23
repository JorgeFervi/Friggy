using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para persistir y consultar unidades de medida.
/// </summary>
public sealed class UnitTypeRepository(FriggyDbContext context) : IUnitTypeRepository
{
    /// <summary>
    /// Obtiene todas las unidades sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<UnitType>> ListAsync(CancellationToken cancellationToken) => await context.UnitTypes.AsNoTracking().ToListAsync(cancellationToken);
    /// <summary>
    /// Busca una unidad por su identificador.
    /// </summary>
    public Task<UnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.UnitTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    /// <summary>
    /// Comprueba si existe una unidad con el nombre normalizado indicado.
    /// </summary>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.UnitTypes.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    public async Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Set<Friggy.Domain.Recipes.RecipeIngredient>()
            .AnyAsync(item => item.UnitTypeId == id, cancellationToken) ||
        await context.InventoryLots.AnyAsync(item => item.UnitTypeId == id, cancellationToken);
    /// <summary>
    /// Añade una unidad al contexto.
    /// </summary>
    public async Task AddAsync(UnitType item, CancellationToken cancellationToken) => await context.UnitTypes.AddAsync(item, cancellationToken);
    /// <summary>
    /// Marca una unidad para eliminarla.
    /// </summary>
    public void Remove(UnitType item) => context.UnitTypes.Remove(item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
