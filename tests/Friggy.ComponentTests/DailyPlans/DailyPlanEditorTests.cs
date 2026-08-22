using Bunit;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.DailyPlans;

namespace Friggy.ComponentTests.DailyPlans;

public sealed class DailyPlanEditorTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void MealSlot_WithPlannedRecipe_UsesModernGreenActionsAndTypedControls()
    {
        var mealTypeId = Guid.NewGuid();
        var meal = new DailyPlanMealResponse(
            mealTypeId,
            "Desayuno",
            0,
            Guid.NewGuid(),
            1,
            false,
            null,
            Guid.NewGuid(),
            0,
            "08:30",
            null,
            MealPlanEntryState.Planned,
            null,
            null);
        var model = new DailyPlanResponse(Guid.NewGuid(), new DateOnly(2026, 8, 22), [meal]);

        var component = Render<DailyPlanEditor>(parameters => parameters
            .Add(item => item.Model, model)
            .Add(item => item.Recipes, [])
            .Add(item => item.MealTypes, []));

        var toolbarActions = component.FindAll(".friggy-meal-slot__header button");
        var completeAction = component.Find("button").Closest("article")!
            .QuerySelector(".friggy-meal-slot__complete");
        var skipAction = component.Find("input[placeholder='Motivo para omitir']")
            .ParentElement!
            .QuerySelector(".friggy-meal-slot__secondary-action");

        Assert.Equal(3, toolbarActions.Count);
        Assert.All(toolbarActions, action =>
            Assert.Contains("friggy-meal-slot__toolbar-action", action.ClassList));
        Assert.NotNull(completeAction);
        Assert.Contains("friggy-button--primary", completeAction.ClassList);
        Assert.NotNull(skipAction);
        Assert.Contains("friggy-button--secondary", skipAction.ClassList);
        Assert.Equal("number", component.Find("input[id^='servings-']").GetAttribute("type"));
        Assert.Equal("time", component.Find("input[id^='time-']").GetAttribute("type"));
    }
}
