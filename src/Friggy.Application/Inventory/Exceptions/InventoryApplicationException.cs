namespace Friggy.Application.Inventory.Exceptions;

public abstract class InventoryApplicationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
