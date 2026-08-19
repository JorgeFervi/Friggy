using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.MealTypes.Interfaces;

/// <summary>
/// Contrato de persistencia para los tipos de comida del catálogo.
/// </summary>
public interface IMealTypeRepository
{
    /// <summary>
    /// Obtiene todos los tipos de comida.
    /// </summary>
    Task<IReadOnlyList<MealType>> ListAsync(CancellationToken cancellationToken);
    /// <summary>
    /// Busca un tipo de comida por su identificador.
    /// </summary>
    Task<MealType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>
    /// Comprueba si ya existe un tipo de comida con el nombre normalizado indicado.
    /// </summary>
    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    /// <summary>
    /// Añade un tipo de comida para su persistencia.
    /// </summary>
    Task AddAsync(MealType item, CancellationToken cancellationToken);
    /// <summary>
    /// Marca un tipo de comida para eliminarlo.
    /// </summary>
    void Remove(MealType item);
    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
