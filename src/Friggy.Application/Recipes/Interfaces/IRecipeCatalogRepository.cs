namespace Friggy.Application.Recipes.Interfaces;

/// <summary>
/// Datos de una unidad necesarios para validar su uso en una receta.
/// </summary>
public sealed record RecipeUnitTypeReference(Guid Id, bool CanUseForCooking);

/// <summary>
/// Contrato para comprobar que las referencias de una receta existen en los
/// catálogos correspondientes.
/// </summary>
public interface IRecipeCatalogRepository
{
    /// <summary>
    /// Comprueba que existan todos los ingredientes indicados.
    /// </summary>
    Task<bool> IngredientsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba que existan todos los tipos de unidad indicados.
    /// </summary>
    Task<bool> UnitTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Obtiene las unidades solicitadas junto con sus capacidades de uso.
    /// </summary>
    Task<IReadOnlyList<RecipeUnitTypeReference>> GetUnitTypesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba que existan todas las etiquetas indicadas.
    /// </summary>
    Task<bool> TagsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Comprueba que existan todos los tipos de comida indicados.
    /// </summary>
    Task<bool> MealTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
