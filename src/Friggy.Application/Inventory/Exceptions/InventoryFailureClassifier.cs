namespace Friggy.Application.Inventory.Exceptions;

public enum InventoryFailureKind
{
    NotFound,
    Conflict,
}

public sealed record InventoryFailure(InventoryFailureKind Kind, string Code);

public static class InventoryFailureClassifier
{
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
