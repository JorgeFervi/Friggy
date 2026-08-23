using Bunit;
using Friggy.Application.DailyPlans.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.DailyPlans;
using Friggy.Web.DailyPlans;

namespace Friggy.ComponentTests.DailyPlans;

public sealed class DailyPlanEditorTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void MealSlot_RecipeSelector_SearchesAndSelectsSingleRecipeWithoutCheckboxes()
    {
        var gazpachoId = Guid.NewGuid();
        var lentilsId = Guid.NewGuid();
        DailyPlanMealChange? observed = null;
        var component = Render<DailyPlanEditor>(parameters => parameters
            .Add(item => item.Model, EmptyPlan())
            .Add(item => item.Recipes,
            [
                new RecipeListItemResponse(gazpachoId, "Gazpacho", 20),
                new RecipeListItemResponse(lentilsId, "Lentejas", 45),
            ])
            .Add(item => item.MealTypes, [])
            .Add(item => item.OnChanged, change => observed = change));

        component.Find("button[data-single-combobox^='recipe-']").Click();
        component.Find("input[data-single-combobox-search^='recipe-']").Input("gaz");

        var options = component.Find("[data-single-combobox-options^='recipe-']");
        Assert.Contains("Gazpacho", options.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Lentejas", options.TextContent, StringComparison.Ordinal);
        Assert.Empty(options.QuerySelectorAll("input[type='checkbox']"));
        options.QuerySelector($"button[data-option-id='{gazpachoId}']")!.Click();

        Assert.NotNull(observed);
        Assert.Equal(gazpachoId, observed.RecipeId);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MealSlot_TimePickerWithSeconds_NormalizesValueBeforeSaving()
    {
        DailyPlanSlotTimeChange? observed = null;
        var component = Render<DailyPlanEditor>(parameters => parameters
            .Add(item => item.Model, EmptyPlan())
            .Add(item => item.Recipes, [])
            .Add(item => item.MealTypes, [])
            .Add(item => item.OnTimeChanged, change => observed = change));

        component.Find("input[id^='time-']").Change("10:52:00");

        Assert.NotNull(observed);
        Assert.Equal("10:52", observed.PlannedTime);
    }

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

    private static DailyPlanResponse EmptyPlan()
    {
        var meal = new DailyPlanMealResponse(
            Guid.NewGuid(),
            "Desayuno",
            0,
            null,
            1,
            false,
            null,
            Guid.NewGuid(),
            0,
            null,
            null,
            MealPlanEntryState.Planned,
            null,
            null);
        return new DailyPlanResponse(Guid.NewGuid(), new DateOnly(2026, 8, 23), [meal]);
    }
}
