using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Inventory;

/// <summary>
/// Clase <see cref="InventoryLot"/> que representa la existencia de un
/// ingrediente agrupada por unidad de medida y fecha de caducidad.
/// </summary>
public sealed class InventoryLot
{
    private readonly List<InventoryMovement> movements = [];

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private InventoryLot()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al lote de forma interna.
    /// </param>
    /// <param name="ingredientId">
    /// Código del ingrediente almacenado.
    /// </param>
    /// <param name="unitTypeId">
    /// Código de la unidad de medida usada por el lote.
    /// </param>
    /// <param name="quantity">
    /// Cantidad disponible en el lote.
    /// </param>
    /// <param name="expirationDate">
    /// Fecha de caducidad del lote.
    /// </param>
    /// <param name="version">
    /// Versión usada para identificar los cambios del lote.
    /// </param>
    private InventoryLot(
        Guid id,
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        DateOnly expirationDate,
        Guid version)
    {
        Id = id;
        IngredientId = ingredientId;
        UnitTypeId = unitTypeId;
        Quantity = quantity;
        ExpirationDate = expirationDate;
        Version = version;
    }

    /// <summary>
    /// Código único para identificar al lote de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código del ingrediente almacenado.
    /// </summary>
    public Guid IngredientId { get; private set; }

    /// <summary>
    /// Código de la unidad de medida usada por el lote.
    /// </summary>
    public Guid UnitTypeId { get; private set; }

    /// <summary>
    /// Cantidad actualmente disponible en el lote.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Fecha de caducidad del lote.
    /// </summary>
    public DateOnly ExpirationDate { get; private set; }

    /// <summary>
    /// Versión usada para identificar los cambios del lote.
    /// </summary>
    public Guid Version { get; private set; }

    /// <summary>
    /// Movimientos registrados para el lote.
    /// </summary>
    public IReadOnlyList<InventoryMovement> Movements => movements.AsReadOnly();

    /// <summary>
    /// Constructor público principal que crea un lote con su movimiento inicial.
    /// </summary>
    /// <param name="ingredientId">
    /// Código del ingrediente almacenado.
    /// </param>
    /// <param name="unitTypeId">
    /// Código de la unidad de medida usada por el lote.
    /// </param>
    /// <param name="quantity">
    /// Cantidad inicial disponible en el lote.
    /// </param>
    /// <param name="expirationDate">
    /// Fecha de caducidad del lote.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró la entrada inicial.
    /// </param>
    /// <returns>
    /// Objeto <see cref="InventoryLot"/>.
    /// </returns>
    public static InventoryLot Create(
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        DateOnly expirationDate,
        DateTimeOffset occurredAt)
    {
        ValidateRequiredId(ingredientId, "inventory-lot.ingredient-id.required");
        ValidateRequiredId(unitTypeId, "inventory-lot.unit-type-id.required");
        ValidatePositive(quantity, "inventory-lot.quantity.positive");

        var lot = new InventoryLot(
            Guid.NewGuid(),
            ingredientId,
            unitTypeId,
            quantity,
            expirationDate,
            Guid.NewGuid());
        lot.movements.Add(InventoryMovement.Create(
            lot.Id,
            InventoryMovementType.InitialStock,
            quantity,
            quantity,
            occurredAt));
        return lot;
    }

    /// <summary>
    /// Método que consume una cantidad del lote y registra el movimiento.
    /// </summary>
    /// <param name="requestedQuantity">
    /// Cantidad que se desea consumir.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró el consumo.
    /// </param>
    /// <param name="mealPlanEntryId">
    /// Código opcional de la comida que originó el consumo.
    /// </param>
    /// <returns>
    /// Resultado con las cantidades aplicada y no aplicada.
    /// </returns>
    public InventoryOperationResult Consume(
        decimal requestedQuantity,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId = null) =>
        Reduce(
            requestedQuantity,
            occurredAt,
            InventoryMovementType.Consumption,
            mealPlanEntryId);

    /// <summary>
    /// Método que descarta una cantidad del lote y registra el movimiento.
    /// </summary>
    /// <param name="requestedQuantity">
    /// Cantidad que se desea descartar.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró el descarte.
    /// </param>
    /// <returns>
    /// Resultado con las cantidades aplicada y no aplicada.
    /// </returns>
    public InventoryOperationResult Discard(
        decimal requestedQuantity,
        DateTimeOffset occurredAt) =>
        Reduce(requestedQuantity, occurredAt, InventoryMovementType.Discard, null);

    /// <summary>
    /// Método que ajusta la cantidad del lote a la existencia real.
    /// </summary>
    /// <param name="actualQuantity">
    /// Cantidad real encontrada para el lote.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró el ajuste.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la cantidad real es negativa.
    /// </exception>
    public void Adjust(decimal actualQuantity, DateTimeOffset occurredAt)
    {
        if (actualQuantity < 0)
        {
            throw new DomainValidationException(
                "inventory-lot.adjustment.non-negative",
                "La cantidad real no puede ser negativa.");
        }

        var delta = actualQuantity - Quantity;
        if (delta == 0)
        {
            return;
        }

        Quantity = actualQuantity;
        RegisterMovement(InventoryMovementType.Adjustment, delta, occurredAt, null);
    }

    /// <summary>
    /// Método para corregir la fecha de caducidad del lote.
    /// </summary>
    /// <param name="expirationDate">
    /// Nueva fecha de caducidad.
    /// </param>
    public void CorrectExpiration(DateOnly expirationDate)
    {
        ExpirationDate = expirationDate;
        Version = Guid.NewGuid();
    }

    /// <summary>
    /// Método que reduce la cantidad disponible y registra un movimiento.
    /// </summary>
    /// <param name="requestedQuantity">
    /// Cantidad que se desea reducir.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró la operación.
    /// </param>
    /// <param name="type">
    /// Tipo de movimiento que se va a registrar.
    /// </param>
    /// <param name="mealPlanEntryId">
    /// Código opcional de la comida que originó la operación.
    /// </param>
    /// <returns>
    /// Resultado con las cantidades aplicada y no aplicada.
    /// </returns>
    private InventoryOperationResult Reduce(
        decimal requestedQuantity,
        DateTimeOffset occurredAt,
        InventoryMovementType type,
        Guid? mealPlanEntryId)
    {
        ValidatePositive(requestedQuantity, "inventory-lot.operation.quantity.positive");

        var applied = Math.Min(Quantity, requestedQuantity);
        var unapplied = requestedQuantity - applied;
        if (applied > 0)
        {
            Quantity -= applied;
            RegisterMovement(type, -applied, occurredAt, mealPlanEntryId);
        }

        return new InventoryOperationResult(applied, unapplied);
    }

    /// <summary>
    /// Método que registra un movimiento y actualiza la versión del lote.
    /// </summary>
    /// <param name="type">
    /// Tipo de movimiento que se va a registrar.
    /// </param>
    /// <param name="delta">
    /// Variación aplicada a la cantidad del lote.
    /// </param>
    /// <param name="occurredAt">
    /// Fecha y hora en la que se registró el movimiento.
    /// </param>
    /// <param name="mealPlanEntryId">
    /// Código opcional de la comida que originó el movimiento.
    /// </param>
    private void RegisterMovement(
        InventoryMovementType type,
        decimal delta,
        DateTimeOffset occurredAt,
        Guid? mealPlanEntryId)
    {
        movements.Add(InventoryMovement.Create(
            Id,
            type,
            delta,
            Quantity,
            occurredAt,
            mealPlanEntryId));
        Version = Guid.NewGuid();
    }

    /// <summary>
    /// Método que comprueba que un identificador sea obligatorio.
    /// </summary>
    /// <param name="id">
    /// Identificador que se va a validar.
    /// </param>
    /// <param name="code">
    /// Código de error que se lanzará si el identificador no es válido.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío.
    /// </exception>
    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }

    /// <summary>
    /// Método que comprueba que una cantidad sea positiva.
    /// </summary>
    /// <param name="quantity">
    /// Cantidad que se va a validar.
    /// </param>
    /// <param name="code">
    /// Código de error que se lanzará si la cantidad no es válida.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la cantidad es cero o negativa.
    /// </exception>
    private static void ValidatePositive(decimal quantity, string code)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException(code, "La cantidad debe ser positiva.");
        }
    }
}
