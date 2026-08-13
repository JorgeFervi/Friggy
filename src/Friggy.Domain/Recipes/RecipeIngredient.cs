using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

public sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    private RecipeIngredient(
        Guid id,
        Guid recipeId,
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        int order)
    {
        Id = id;
        RecipeId = recipeId;
        IngredientId = ingredientId;
        UnitTypeId = unitTypeId;
        Quantity = quantity;
        Order = order;
    }

    public Guid Id { get; private set; }

    public Guid RecipeId { get; private set; }

    public Guid IngredientId { get; private set; }

    public Guid UnitTypeId { get; private set; }

    public decimal Quantity { get; private set; }

    public int Order { get; private set; }

    internal static RecipeIngredient Create(
        Guid recipeId,
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        int order,
        Guid? id = null)
    {
        ValidateRequiredId(recipeId, "recipe-ingredient.recipe-id.required");
        ValidateRequiredId(ingredientId, "recipe-ingredient.ingredient-id.required");
        ValidateRequiredId(unitTypeId, "recipe-ingredient.unit-type-id.required");
        if (id.HasValue)
        {
            ValidateRequiredId(id.Value, "recipe-ingredient.id.required");
        }

        if (quantity <= 0)
        {
            throw new DomainValidationException(
                "recipe-ingredient.quantity.positive",
                "La cantidad debe ser positiva.");
        }

        if (order < 0)
        {
            throw new DomainValidationException(
                "recipe-ingredient.order.non-negative",
                "La posición no puede ser negativa.");
        }

        return new RecipeIngredient(
            id ?? Guid.NewGuid(),
            recipeId,
            ingredientId,
            unitTypeId,
            quantity,
            order);
    }

    internal void UpdateFrom(RecipeIngredient replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        if (Id != replacement.Id)
        {
            throw new ArgumentException(
                "La línea de reemplazo debe conservar la misma identidad.",
                nameof(replacement));
        }

        IngredientId = replacement.IngredientId;
        UnitTypeId = replacement.UnitTypeId;
        Quantity = replacement.Quantity;
        Order = replacement.Order;
    }

    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }
}
