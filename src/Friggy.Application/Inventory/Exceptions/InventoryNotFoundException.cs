namespace Friggy.Application.Inventory.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró un lote o recurso de inventario.
/// </summary>
public sealed class InventoryNotFoundException(string code, string message)
    : InventoryApplicationException(code, message);
