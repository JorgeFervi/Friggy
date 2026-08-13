namespace Friggy.Web.Recipes;

public sealed class RecipeStepFormModel
{
    public RecipeStepFormModel()
    {
    }

    public RecipeStepFormModel(
        string description,
        int? estimatedMinutes,
        int order,
        IEnumerable<Guid>? recipeIngredientIds = null)
    {
        Description = description;
        EstimatedMinutes = estimatedMinutes;
        Order = order;
        RecipeIngredientIds.UnionWith(recipeIngredientIds ?? []);
    }

    public Guid ClientId { get; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public int? EstimatedMinutes { get; set; }
    public int Order { get; set; }
    public HashSet<Guid> RecipeIngredientIds { get; } = [];
}
