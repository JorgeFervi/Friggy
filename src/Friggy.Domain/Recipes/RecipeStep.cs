using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeStep"/> que representa un paso de preparación
/// dentro de una receta.
/// </summary>
public sealed class RecipeStep
{
    private readonly List<RecipeStepIngredientLink> ingredientLinks = [];

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeStep()
    {
        Description = string.Empty;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al paso de forma interna.
    /// </param>
    /// <param name="recipeId">
    /// Código de la receta a la que pertenece el paso.
    /// </param>
    /// <param name="description">
    /// Descripción del paso.
    /// </param>
    /// <param name="estimatedTime">
    /// Tiempo estimado del paso.
    /// </param>
    /// <param name="order">
    /// Posición del paso dentro de la receta.
    /// </param>
    private RecipeStep(
        Guid id,
        Guid recipeId,
        string description,
        TimeSpan? estimatedTime,
        int order)
    {
        Id = id;
        RecipeId = recipeId;
        Description = description;
        EstimatedTime = estimatedTime;
        Order = order;
    }

    /// <summary>
    /// Código único para identificar al paso de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Código de la receta a la que pertenece el paso.
    /// </summary>
    public Guid RecipeId { get; private set; }

    /// <summary>
    /// Descripción del paso.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Tiempo estimado del paso.
    /// </summary>
    public TimeSpan? EstimatedTime { get; private set; }

    /// <summary>
    /// Posición del paso dentro de la receta.
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Asociaciones entre el paso y sus líneas de ingrediente.
    /// </summary>
    public IReadOnlyList<RecipeStepIngredientLink> IngredientLinks =>
        ingredientLinks.AsReadOnly();

    /// <summary>
    /// Códigos de las líneas de ingrediente asociadas al paso.
    /// </summary>
    public IReadOnlyList<Guid> RecipeIngredientIds =>
        ingredientLinks.Select(item => item.RecipeIngredientId).ToArray();

    /// <summary>
    /// Constructor interno principal.
    /// </summary>
    /// <param name="recipeId">
    /// Código de la receta a la que pertenece el paso.
    /// </param>
    /// <param name="description">
    /// Descripción del paso.
    /// </param>
    /// <param name="estimatedTime">
    /// Tiempo estimado del paso.
    /// </param>
    /// <param name="order">
    /// Posición del paso dentro de la receta.
    /// </param>
    /// <param name="id">
    /// Código opcional del paso.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeStep"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador o la descripción
    /// están vacíos, el tiempo es negativo o la posición es negativa.
    /// </exception>
    internal static RecipeStep Create(
        Guid recipeId,
        string? description,
        TimeSpan? estimatedTime,
        int order,
        Guid? id = null)
    {
        if (recipeId == Guid.Empty)
        {
            throw new DomainValidationException(
                "recipe-step.recipe-id.required",
                "El identificador de la receta es obligatorio.");
        }

        if (id == Guid.Empty)
        {
            throw new DomainValidationException(
                "recipe-step.id.required",
                "El identificador del paso es obligatorio.");
        }

        var trimmedDescription = description?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedDescription))
        {
            throw new DomainValidationException(
                "recipe-step.description.required",
                "La descripción del paso es obligatoria.");
        }

        if (estimatedTime < TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "recipe-step.estimated-time.non-negative",
                "El tiempo estimado del paso no puede ser negativo.");
        }

        if (order < 0)
        {
            throw new DomainValidationException(
                "recipe-step.order.non-negative",
                "La posición no puede ser negativa.");
        }

        return new RecipeStep(
            id ?? Guid.NewGuid(),
            recipeId,
            trimmedDescription,
            estimatedTime,
            order);
    }

    /// <summary>
    /// Método que asocia una línea de ingrediente al paso.
    /// </summary>
    /// <param name="ingredient">
    /// Línea de ingrediente que se va a asociar.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la línea de ingrediente es nula.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando la línea ya está asociada al paso.
    /// </exception>
    internal void AssignIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        if (ingredientLinks.Any(item => item.RecipeIngredientId == ingredient.Id))
        {
            throw new RecipeConflictException(
                "recipe-step.ingredient.duplicate",
                "La línea de ingrediente ya está asociada al paso.");
        }

        ingredientLinks.Add(RecipeStepIngredientLink.Create(this, ingredient));
    }

    /// <summary>
    /// Método que retira una línea de ingrediente del paso.
    /// </summary>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la asociación existía y se retiró; en caso
    /// contrario, <see langword="false"/>.
    /// </returns>
    internal bool RemoveIngredient(Guid recipeIngredientId)
    {
        var link = ingredientLinks.SingleOrDefault(item =>
            item.RecipeIngredientId == recipeIngredientId);
        return link is not null && ingredientLinks.Remove(link);
    }

    /// <summary>
    /// Método que elimina todas las asociaciones de ingredientes del paso.
    /// </summary>
    internal void ClearIngredientAssociations() => ingredientLinks.Clear();
}
