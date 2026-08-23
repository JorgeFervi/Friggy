namespace Friggy.Web.Recipes;

public sealed class RecipeIngredientFormModel
{
    public RecipeIngredientFormModel()
    {
    }

    public RecipeIngredientFormModel(
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        int order,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        IsPersisted = id.HasValue;
        IngredientId = ingredientId;
        UnitTypeId = unitTypeId;
        Quantity = quantity;
        Order = order;
    }

    public Guid ClientId { get; } = Guid.NewGuid();
    public Guid Id { get; private set; } = Guid.NewGuid();
    public bool IsPersisted { get; }
    public Guid IngredientId { get; set; }
    public Guid UnitTypeId { get; set; }
    public decimal Quantity { get; set; }
    public int Order { get; set; }
}
