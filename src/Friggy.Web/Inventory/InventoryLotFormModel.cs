using System.ComponentModel.DataAnnotations;
using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Inventory;

public sealed class InventoryLotFormModel : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona un ingrediente.")]
    public Guid? IngredientId { get; set; }

    [Required(ErrorMessage = "Selecciona una unidad.")]
    public Guid? UnitTypeId { get; set; }

    public decimal Quantity { get; set; }

    public DateOnly ExpirationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public CreateInventoryLotRequest ToRequest() =>
        new(
            IngredientId ?? Guid.Empty,
            UnitTypeId ?? Guid.Empty,
            Quantity,
            ExpirationDate);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
        {
            yield return new ValidationResult(
                "La cantidad debe ser positiva.",
                [nameof(Quantity)]);
        }
    }
}
