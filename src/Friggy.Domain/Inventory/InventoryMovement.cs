namespace Friggy.Domain.Inventory;

/// <summary>
/// Clase <see cref="InventoryMovement"/> que representa una variación
/// registrada sobre la cantidad de un lote de inventario.
/// </summary>
public sealed class InventoryMovement
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private InventoryMovement()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al movimiento de forma interna.
    /// </param>
    /// <param name="inventoryLotId">
    /// Código del lote afectado.
    /// </param>
    /// <param name="type">
    /// Tipo de movimiento.
    /// </param>
    /// <param name="delta">
    /// Variación aplicada a la cantidad del lote.
    /// </param>
    /// <param name="resultingQuantity">
    /// Cantidad del lote después del movimiento.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que ocurrió el movimiento.
    /// </param>
    /// <param name="mealPlanEntryId">
    /// Código opcional de la comida asociada al movimiento.
    /// </param>
    private InventoryMovement(
        Guid id,
        Guid inventoryLotId,
        InventoryMovementType type,
        decimal delta,
        decimal resultingQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId)
    {
        Id = id;
        InventoryLotId = inventoryLotId;
        Type = type;
        Delta = delta;
        ResultingQuantity = resultingQuantity;
        OccurredAt = occurredAt;
        MealPlanEntryId = mealPlanEntryId;
    }

    /// <summary>
    /// Código único para identificar al movimiento de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código del lote afectado.
    /// </summary>
    public Guid InventoryLotId { get; private set; }

    /// <summary>
    /// Tipo de movimiento.
    /// </summary>
    public InventoryMovementType Type { get; private set; }

    /// <summary>
    /// Variación aplicada a la cantidad del lote.
    /// </summary>
    public decimal Delta { get; private set; }

    /// <summary>
    /// Cantidad del lote después del movimiento.
    /// </summary>
    public decimal ResultingQuantity { get; private set; }

    /// <summary>
    /// Fecha y hora en la que ocurrió el movimiento.
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Código opcional de la comida asociada al movimiento.
    /// </summary>
    public Guid? MealPlanEntryId { get; private set; }

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="inventoryLotId">
    /// Código del lote afectado.
    /// </param>
    /// <param name="type">
    /// Tipo de movimiento.
    /// </param>
    /// <param name="delta">
    /// Variación aplicada a la cantidad del lote.
    /// </param>
    /// <param name="resultingQuantity">
    /// Cantidad del lote después del movimiento.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que ocurrió el movimiento.
    /// </param>
    /// <param name="mealPlanEntryId">
    /// Código opcional de la comida asociada al movimiento.
    /// </param>
    /// <returns>
    /// Objeto <see cref="InventoryMovement"/>.
    /// </returns>
    internal static InventoryMovement Create(
        Guid inventoryLotId,
        InventoryMovementType type,
        decimal delta,
        decimal resultingQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId = null) =>
        new(
            Guid.NewGuid(),
            inventoryLotId,
            type,
            delta,
            resultingQuantity,
            occurredAt,
            mealPlanEntryId);
}
