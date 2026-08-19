using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para persistir y consultar etiquetas de receta.
/// </summary>
public sealed class RecipeTagRepository(FriggyDbContext context) : IRecipeTagRepository
{
    /// <summary>
    /// Obtiene todas las etiquetas sin seguimiento de cambios.
    /// </summary>
    public async Task<IReadOnlyList<RecipeTag>> ListAsync(CancellationToken cancellationToken) => await context.RecipeTags.AsNoTracking().ToListAsync(cancellationToken);
    /// <summary>
    /// Busca una etiqueta por su identificador.
    /// </summary>
    public Task<RecipeTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.RecipeTags.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    /// <summary>
    /// Comprueba si existe una etiqueta con el nombre normalizado indicado.
    /// </summary>
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.RecipeTags.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    /// <summary>
    /// Añade una etiqueta al contexto.
    /// </summary>
    public async Task AddAsync(RecipeTag item, CancellationToken cancellationToken) => await context.RecipeTags.AddAsync(item, cancellationToken);
    /// <summary>
    /// Marca una etiqueta para eliminarla.
    /// </summary>
    public void Remove(RecipeTag item) => context.RecipeTags.Remove(item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
