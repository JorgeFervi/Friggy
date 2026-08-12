namespace Friggy.Application.Inventory.Exceptions;

public sealed class InventoryReferenceNotFoundException(string code, string message)
    : InventoryApplicationException(code, message);
