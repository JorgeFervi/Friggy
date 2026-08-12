namespace Friggy.Application.Inventory.Exceptions;

public sealed class InventoryConflictException(string code, string message)
    : InventoryApplicationException(code, message);
