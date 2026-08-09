using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

public sealed class RecipeStep
{
    private RecipeStep()
    {
        Description = string.Empty;
    }

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

    public Guid Id { get; private set; }

    public Guid RecipeId { get; private set; }

    public string Description { get; private set; }

    public TimeSpan? EstimatedTime { get; private set; }

    public int Order { get; private set; }

    internal static RecipeStep Create(
        Guid recipeId,
        string? description,
        TimeSpan? estimatedTime,
        int order)
    {
        if (recipeId == Guid.Empty)
        {
            throw new DomainValidationException(
                "recipe-step.recipe-id.required",
                "El identificador de la receta es obligatorio.");
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
            Guid.NewGuid(),
            recipeId,
            trimmedDescription,
            estimatedTime,
            order);
    }
}
