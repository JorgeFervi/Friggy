using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para persistir y consultar ingredientes.
/// </summary>
public sealed class IngredientRepository(FriggyDbContext context) : IIngredientRepository
{
    /// <summary>
    /// Obtiene todos los ingredientes sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken) =>
        await context.Ingredients.AsNoTracking().ToListAsync(cancellationToken);
    /// <summary>
    /// Busca un ingrediente por su identificador.
    /// </summary>
    public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Ingredients.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    /// <summary>
    /// Comprueba si existe un ingrediente con el nombre normalizado indicado.
    /// </summary>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) =>
        context.Ingredients.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    /// <summary>
    /// Añade un ingrediente al contexto.
    /// </summary>
    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken) => await context.Ingredients.AddAsync(ingredient, cancellationToken);
    /// <summary>
    /// Marca un ingrediente para eliminarlo.
    /// </summary>
    public void Remove(Ingredient ingredient) => context.Ingredients.Remove(ingredient);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
