namespace Friggy.Web.Recipes;

public sealed class RecipeStepFormModel
{
    public RecipeStepFormModel()
    {
    }

    public RecipeStepFormModel(string description, int? estimatedMinutes, int order)
    {
        Description = description;
        EstimatedMinutes = estimatedMinutes;
        Order = order;
    }

    public Guid ClientId { get; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public int? EstimatedMinutes { get; set; }
    public int Order { get; set; }
}
