using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeIngredient"/> que representa una línea de ingrediente
/// dentro de una receta.
/// </summary>
public sealed class RecipeIngredient
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeIngredient()
    {
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar la línea de forma interna.
    /// </param>
    /// <param name="recipeId">
    /// Código de la receta a la que pertenece la línea.
    /// </param>
    /// <param name="ingredientId">
    /// Código del ingrediente.
    /// </param>
    /// <param name="unitTypeId">
    /// Código de la unidad de medida.
    /// </param>
    /// <param name="quantity">
    /// Cantidad del ingrediente.
    /// </param>
    /// <param name="order">
    /// Posición de la línea dentro de la receta.
    /// </param>
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

    /// <summary>
    /// Código único para identificar la línea de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código de la receta a la que pertenece la línea.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Código del ingrediente.
    /// </summary>
    public Guid IngredientId { get; private set; }

    /// <summary>
    /// Código de la unidad de medida.
    /// </summary>
    public Guid UnitTypeId { get; private set; }

    /// <summary>
    /// Cantidad del ingrediente.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Posición de la línea dentro de la receta.
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta a la que pertenece la línea.
    /// </param>
    /// <param name="ingredientId">
    /// Código del ingrediente.
    /// </param>
    /// <param name="unitTypeId">
    /// Código de la unidad de medida.
    /// </param>
    /// <param name="quantity">
    /// Cantidad del ingrediente.
    /// </param>
    /// <param name="order">
    /// Posición de la línea dentro de la receta.
    /// </param>
    /// <param name="id">
    /// Código opcional de la línea de ingrediente.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeIngredient"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando alguno de los identificadores está
    /// vacío, la cantidad no es positiva o la posición es negativa.
    /// </exception>
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

    /// <summary>
    /// Método que actualiza la línea con los valores de otra línea que conserva
    /// la misma identidad.
    /// </summary>
    /// <param name="replacement">
    /// Línea cuyos valores se van a aplicar.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la línea de reemplazo es nula.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Excepción lanzada cuando la línea de reemplazo tiene otra identidad.
    /// </exception>
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
}
