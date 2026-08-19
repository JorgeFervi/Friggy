namespace Friggy.Application.Inventory.Dtos;

/// <summary>
/// Enumeración con los tipos de movimiento expuestos por la aplicación.
/// </summary>
public enum InventoryMovementKind
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
    /// Ajuste de existencias.
    /// </summary>
    Adjustment = 2,
    /// <summary>
    /// Descarte de existencias.
    /// </summary>
    Discard = 3,
}
