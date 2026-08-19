using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.RecipeTags.Interfaces;

/// <summary>
/// Contrato de persistencia para las etiquetas de receta.
/// </summary>
public interface IRecipeTagRepository
{
    /// <summary>
    /// Obtiene todas las etiquetas de receta.
    /// </summary>
    Task<IReadOnlyList<RecipeTag>> ListAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Busca una etiqueta de receta por su identificador.
    /// </summary>
    Task<RecipeTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>
    /// Comprueba si ya existe una etiqueta con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    /// <summary>
    /// Añade una etiqueta para su persistencia.
    /// </summary>
    Task AddAsync(RecipeTag item, CancellationToken cancellationToken);
    /// <summary>
    /// Marca una etiqueta para eliminarla.
    /// </summary>
    void Remove(RecipeTag item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
