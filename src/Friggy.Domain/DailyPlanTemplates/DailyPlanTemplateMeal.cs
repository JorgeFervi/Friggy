using Friggy.Domain.Catalogs;

namespace Friggy.Domain.DailyPlanTemplates;

/// <summary>Configuración reutilizable de una comida dentro de una plantilla.</summary>
public sealed class DailyPlanTemplateMeal
{
    private DailyPlanTemplateMeal()
    {
    }

    private DailyPlanTemplateMeal(
        Guid id,
        Guid dailyPlanTemplateId,
        Guid mealTypeId,
        Guid? recipeId,
        int servings,
        TimeOnly? plannedTime,
        int order)
    {
        Id = id;
        DailyPlanTemplateId = dailyPlanTemplateId;
        MealTypeId = mealTypeId;
        RecipeId = recipeId;
        Servings = servings;
        PlannedTime = plannedTime;
        Order = order;
    }

    public Guid Id { get; private set; }

    public Guid DailyPlanTemplateId { get; private set; }

    public Guid MealTypeId { get; private set; }

    public Guid? RecipeId { get; private set; }

    public int Servings { get; private set; }

    public TimeOnly? PlannedTime { get; private set; }

    public int Order { get; private set; }

    internal static DailyPlanTemplateMeal Create(
        Guid dailyPlanTemplateId,
        Guid mealTypeId,
        Guid? recipeId,
        int servings,
        TimeOnly? plannedTime,
        int order) =>
        new(Guid.NewGuid(), dailyPlanTemplateId, mealTypeId, recipeId, servings, plannedTime, order);

    internal void MoveTo(int order) => Order = order;
}
