using Friggy.Application.Recipes.Dtos;
using Friggy.Domain.Recipes;

namespace Friggy.Application.Recipes.Interfaces;

/// <summary>
/// Contrato de persistencia para las recetas.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>
    /// Obtiene todas las recetas completas.
    /// </summary>
    Task<IReadOnlyList<Recipe>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene los datos resumidos de las recetas para listados.
    /// </summary>
    Task<IReadOnlyList<RecipeListItemResponse>> ListSummariesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca una receta por su identificador.
    /// </summary>
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba si ya existe una receta con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Añade una receta para su persistencia.
    /// </summary>
    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);

    /// <summary>
    /// Marca una receta para eliminarla.
    /// </summary>
    void Remove(Recipe recipe);

    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
