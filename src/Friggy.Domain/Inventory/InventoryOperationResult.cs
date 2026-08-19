namespace Friggy.Domain.Inventory;

/// <summary>
/// Resultado de una operación de reducción de existencias.
/// </summary>
/// <param name="AppliedQuantity">
/// Cantidad que se pudo aplicar sobre el lote.
/// </param>
/// <param name="UnappliedQuantity">
/// Cantidad solicitada que no se pudo aplicar por falta de existencias.
/// </param>
public readonly record struct InventoryOperationResult(
    decimal AppliedQuantity,
    decimal UnappliedQuantity);
