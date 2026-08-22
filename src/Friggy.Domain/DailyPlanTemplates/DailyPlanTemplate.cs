using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;

namespace Friggy.Domain.DailyPlanTemplates;

/// <summary>Configuración reutilizable para materializar planes diarios futuros.</summary>
public sealed class DailyPlanTemplate
{
    private readonly List<DailyPlanTemplateMeal> meals = [];

    private DailyPlanTemplate()
    {
        Name = null!;
    }

    private DailyPlanTemplate(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public IReadOnlyList<DailyPlanTemplateMeal> Meals => meals.AsReadOnly();

    public static DailyPlanTemplate Create(string? name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "daily-plan-template.name.required"));

    public void Rename(string? name) =>
        Name = CatalogName.Create(name, "daily-plan-template.name.required");

    public void ReplaceMeals(IReadOnlyList<DailyPlanTemplateMealDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var ordered = definitions.OrderBy(item => item.Order).ToArray();
        if (ordered.Select(item => item.Order).SequenceEqual(Enumerable.Range(0, ordered.Length)) is false ||
            ordered.Select(item => item.MealTypeId).Distinct().Count() != ordered.Length)
        {
            throw new DomainValidationException(
                "daily-plan-template.meal.order.invalid",
                "Las comidas deben usar tipos únicos y un orden continuo desde cero.");
        }

        foreach (var item in ordered)
        {
            ValidateMeal(item.MealTypeId, item.RecipeId, item.Servings);
        }

        meals.Clear();
        foreach (var item in ordered)
        {
            meals.Add(DailyPlanTemplateMeal.Create(
                Id,
                item.MealTypeId,
                item.RecipeId,
                item.Servings,
                item.PlannedTime,
                item.Order));
        }
    }

    public DailyPlanTemplateMeal AddMeal(
        Guid mealTypeId,
        Guid? recipeId,
        int servings = 1,
        TimeOnly? plannedTime = null)
    {
        ValidateMeal(mealTypeId, recipeId, servings);
        if (meals.Any(meal => meal.MealTypeId == mealTypeId))
        {
            throw new DomainValidationException(
                "daily-plan-template.meal-type.duplicate",
                "El tipo de comida ya existe en la plantilla.");
        }

        var meal = DailyPlanTemplateMeal.Create(
            Id,
            mealTypeId,
            recipeId,
            servings,
            plannedTime,
            meals.Count);
        meals.Add(meal);
        return meal;
    }

    public DailyPlan Instantiate(DateOnly date)
    {
        var plan = DailyPlan.Create(date);
        foreach (var meal in meals.OrderBy(item => item.Order))
        {
            var slot = plan.AddSlot(meal.MealTypeId);
            plan.SetSlotTime(slot.Id, meal.PlannedTime);
            if (meal.RecipeId.HasValue)
            {
                plan.Assign(meal.MealTypeId, meal.RecipeId.Value, meal.Servings);
            }
        }

        return plan;
    }

    private static void ValidateMeal(Guid mealTypeId, Guid? recipeId, int servings)
    {
        if (mealTypeId == Guid.Empty)
        {
            throw new DomainValidationException(
                "daily-plan-template.meal-type-id.required",
                "El tipo de comida es obligatorio.");
        }

        if (recipeId == Guid.Empty)
        {
            throw new DomainValidationException(
                "daily-plan-template.recipe-id.required",
                "La receta no puede estar vacía.");
        }

        if (servings <= 0)
        {
            throw new DomainValidationException(
                "daily-plan-template.meal.servings.positive",
                "Las raciones deben ser mayores que cero.");
        }
    }
}

/// <summary>Valores editables de una comida de plantilla.</summary>
public sealed record DailyPlanTemplateMealDefinition(
    Guid MealTypeId,
    Guid? RecipeId,
    int Servings,
    TimeOnly? PlannedTime,
    int Order);
