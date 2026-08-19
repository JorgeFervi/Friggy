namespace Friggy.Application.Inventory.Exceptions;

/// <summary>
/// Enumeración con los tipos de fallo que pueden producirse en inventario.
/// </summary>
public enum InventoryFailureKind
{
    /// <summary>
    /// Fallo causado porque no se encontró un recurso.
    /// </summary>
    NotFound,
    /// <summary>
    /// Fallo causado por un conflicto de negocio.
    /// </summary>
    Conflict,
}

/// <summary>
/// Resultado clasificado de un fallo producido en inventario.
/// </summary>
public sealed record InventoryFailure(InventoryFailureKind Kind, string Code);

/// <summary>
/// Clase que traduce excepciones de inventario a fallos reconocibles por la API.
/// </summary>
public static class InventoryFailureClassifier
{
    /// <summary>
    /// Método que clasifica una excepción relacionada con inventario.
    /// </summary>
    /// <param name="exception">
    /// Excepción que se va a clasificar.
    /// </param>
    /// <returns>
    /// Fallo clasificado o <see langword="null"/> cuando la excepción no se
    /// reconoce como un fallo de inventario.
    /// </returns>
    public static InventoryFailure? Classify(Exception exception) => exception switch
    {
        InventoryNotFoundException notFound =>
            new(InventoryFailureKind.NotFound, notFound.Code),
        InventoryReferenceNotFoundException reference =>
            new(InventoryFailureKind.NotFound, reference.Code),
        InventoryConflictException conflict =>
            new(InventoryFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
