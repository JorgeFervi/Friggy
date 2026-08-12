namespace Friggy.Application.Inventory.Exceptions;

public sealed class InventoryNotFoundException(string code, string message)
    : InventoryApplicationException(code, message);
