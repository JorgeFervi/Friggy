using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.Ingredients.Interfaces;

/// <summary>
/// Contrato de persistencia para los ingredientes del catálogo.
/// </summary>
public interface IIngredientRepository
{
    /// <summary>
    /// Obtiene todos los ingredientes.
    /// </summary>
    Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Busca un ingrediente por su identificador.
    /// </summary>
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>
    /// Comprueba si ya existe un ingrediente con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    /// <summary>
    /// Añade un ingrediente para su persistencia.
    /// </summary>
    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken);
    /// <summary>
    /// Marca un ingrediente para eliminarlo.
    /// </summary>
    void Remove(Ingredient ingredient);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
