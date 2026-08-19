namespace Friggy.Application.Inventory.Exceptions;

/// <summary>
/// Excepción que indica un conflicto al operar sobre el inventario.
/// </summary>
public sealed class InventoryConflictException(string code, string message)
    : InventoryApplicationException(code, message);
