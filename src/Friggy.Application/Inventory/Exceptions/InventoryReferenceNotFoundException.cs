namespace Friggy.Application.Inventory.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró una referencia necesaria para
/// operar sobre el inventario.
/// </summary>
public sealed class InventoryReferenceNotFoundException(string code, string message)
    : InventoryApplicationException(code, message);
