using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para persistir y consultar tipos de comida.
/// </summary>
public sealed class MealTypeRepository(FriggyDbContext context) : IMealTypeRepository
{
    /// <summary>
    /// Obtiene todos los tipos de comida sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<MealType>> ListAsync(CancellationToken cancellationToken) => await context.MealTypes.AsNoTracking().ToListAsync(cancellationToken);
    /// <summary>
    /// Busca un tipo de comida por su identificador.
    /// </summary>
    public Task<MealType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.MealTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    /// <summary>
    /// Comprueba si existe un tipo de comida con el nombre normalizado indicado.
    /// </summary>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.MealTypes.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    /// <summary>
    /// Añade un tipo de comida al contexto.
    /// </summary>
    public async Task AddAsync(MealType item, CancellationToken cancellationToken) => await context.MealTypes.AddAsync(item, cancellationToken);
    /// <summary>
    /// Marca un tipo de comida para eliminarlo.
    /// </summary>
    public void Remove(MealType item) => context.MealTypes.Remove(item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
