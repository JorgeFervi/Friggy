using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlanTemplates;

namespace Friggy.Domain.Tests.DailyPlanTemplates;

public sealed class DailyPlanTemplateTests
{
    [Fact]
    public void Instantiate_ConfiguredTemplate_CreatesIndependentDailyPlan()
    {
        var template = DailyPlanTemplate.Create("Día de entrenamiento");
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        template.AddMeal(mealTypeId, recipeId, servings: 2, new TimeOnly(14, 30));

        var first = template.Instantiate(new DateOnly(2030, 1, 8));
        var second = template.Instantiate(new DateOnly(2030, 1, 10));

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.Slots.Single().Id, second.Slots.Single().Id);
        Assert.NotEqual(first.Entries.Single().Id, second.Entries.Single().Id);
        Assert.Equal(2, first.Entries.Single().Servings);
        Assert.Equal(new TimeOnly(14, 30), first.Slots.Single().PlannedTime);
    }

    [Fact]
    public void AddMeal_DuplicateMealType_ThrowsWithoutMutation()
    {
        var template = DailyPlanTemplate.Create("Laborable");
        var mealTypeId = Guid.NewGuid();
        template.AddMeal(mealTypeId, null, 1, null);

        var exception = Assert.Throws<DomainValidationException>(() =>
            template.AddMeal(mealTypeId, null, 1, null));

        Assert.Equal("daily-plan-template.meal-type.duplicate", exception.Code);
        Assert.Single(template.Meals);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddMeal_NonPositiveServings_ThrowsWithoutMutation(int servings)
    {
        var template = DailyPlanTemplate.Create("Laborable");

        var exception = Assert.Throws<DomainValidationException>(() =>
            template.AddMeal(Guid.NewGuid(), null, servings, null));

        Assert.Equal("daily-plan-template.meal.servings.positive", exception.Code);
        Assert.Empty(template.Meals);
    }
}
