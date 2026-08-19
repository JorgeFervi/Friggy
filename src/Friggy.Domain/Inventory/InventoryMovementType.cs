namespace Friggy.Domain.Inventory;

/// <summary>
/// Enumeración con los tipos de movimiento que pueden registrarse en un lote.
/// </summary>
public enum InventoryMovementType
{
    /// <summary>
    /// Entrada inicial de existencias.
    /// </summary>
    InitialStock = 0,
    /// <summary>
    /// Consumo de existencias.
    /// </summary>
    Consumption = 1,
    /// <summary>
    /// Ajuste de la cantidad real de existencias.
    /// </summary>
    Adjustment = 2,
    /// <summary>
    /// Descarte de existencias.
    /// </summary>
    Discard = 3,
}
