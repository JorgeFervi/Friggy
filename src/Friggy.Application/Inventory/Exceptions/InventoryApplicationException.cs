namespace Friggy.Application.Inventory.Exceptions;

/// <summary>
/// Clase base para los errores producidos al ejecutar casos de uso de inventario.
/// </summary>
public abstract class InventoryApplicationException(string code, string message)
    : Exception(message)
{
    /// <summary>
    /// Código que identifica el fallo de aplicación.
    /// </summary>
    public string Code { get; } = code;
}
