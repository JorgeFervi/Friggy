using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.Recipes;
using Friggy.Web.Recipes;

namespace Friggy.ComponentTests.Recipes;

public sealed class RecipeFormTests : ComponentTest
{
    private static readonly Guid IngredientOneId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid IngredientTwoId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid UnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TagOneId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid TagTwoId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    [Fact]
    [Trait("Category", "Component")]
    public void RemoveIngredient_TwoRows_RemovesOnlySelectedRowAndReindexesOrder()
    {
        var first = new RecipeIngredientFormModel(IngredientOneId, UnitTypeId, 1m, 0);
        var second = new RecipeIngredientFormModel(IngredientTwoId, UnitTypeId, 2m, 1);
        var model = ValidModel();
        model.Ingredients.Add(first);
        model.Ingredients.Add(second);
        var component = RenderForm(model);

        component.Find($"button[aria-label='Eliminar ingrediente 1']").Click();

        var row = Assert.Single(component.FindAll("[data-testid='ingredient-row']"));
        Assert.Contains("Pepino", row.TextContent, StringComparison.Ordinal);
        Assert.Equal(second.ClientId, Assert.Single(model.Ingredients).ClientId);
        Assert.Equal(0, model.Ingredients[0].Order);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MoveStep_SecondRowUp_PreservesStableKeysAndUpdatesOrder()
    {
        var first = new RecipeStepFormModel("Triturar", null, 0);
        var second = new RecipeStepFormModel("Servir", 2, 1);
        var model = ValidModel();
        model.Steps.Add(first);
        model.Steps.Add(second);
        var component = RenderForm(model);

        component.Find("button[aria-label='Mover paso 2 arriba']").Click();

        Assert.Equal([second.ClientId, first.ClientId], model.Steps.Select(item => item.ClientId));
        Assert.Equal([0, 1], model.Steps.Select(item => item.Order));
        Assert.Equal(
            ["Servir", "Triturar"],
            component.FindAll("[data-testid='step-row'] textarea")
                .Select(element => element.GetAttribute("value") ?? element.TextContent));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void SelectTags_TwoCheckedValues_PreservesBothSelections()
    {
        var model = ValidModel();
        var component = RenderForm(model);

        component.Find($"#tag-{TagOneId}").Change(true);
        component.Find($"#tag-{TagTwoId}").Change(true);

        Assert.Equal([TagOneId, TagTwoId], model.TagIds.Order());
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Submit_InvalidTimeAndQuantity_ShowsValidationAndDoesNotSave()
    {
        var saved = false;
        var model = ValidModel();
        model.EstimatedMinutes = 0;
        model.Ingredients.Add(new(IngredientOneId, UnitTypeId, 0m, 0));
        var component = RenderForm(model, () => saved = true);

        component.Find("form").Submit();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("mayor que cero", component.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cantidad", component.Markup, StringComparison.OrdinalIgnoreCase);
        });
        Assert.False(saved);
    }

    [Theory]
    [InlineData(false, false, "Guardar receta")]
    [InlineData(true, true, "Guardando")]
    [Trait("Category", "Component")]
    public void SavingState_ControlsSubmitAvailabilityAndText(
        bool isSaving,
        bool expectedDisabled,
        string expectedText)
    {
        var component = RenderForm(ValidModel(), isSaving: isSaving);

        var submit = component.Find("button[type='submit']");

        Assert.Equal(expectedDisabled, submit.HasAttribute("disabled"));
        Assert.Contains(expectedText, submit.TextContent, StringComparison.Ordinal);
    }

    private IRenderedComponent<RecipeForm> RenderForm(
        RecipeFormModel model,
        Action? onValidSubmit = null,
        bool isSaving = false) =>
        Render<RecipeForm>(parameters =>
        {
            parameters
                .Add(component => component.Model, model)
                .Add(component => component.Ingredients, Ingredients)
                .Add(component => component.UnitTypes, UnitTypes)
                .Add(component => component.Tags, Tags)
                .Add(component => component.MealTypes, MealTypes)
                .Add(component => component.IsSaving, isSaving);
            if (onValidSubmit is not null)
            {
                parameters.Add(component => component.OnValidSubmit, onValidSubmit);
            }
        });

    private static RecipeFormModel ValidModel() => new()
    {
        Name = "Gazpacho",
        EstimatedMinutes = 20,
    };

    private static IReadOnlyList<IngredientResponse> Ingredients =>
        [new(IngredientOneId, "Tomate"), new(IngredientTwoId, "Pepino")];

    private static IReadOnlyList<UnitTypeResponse> UnitTypes =>
        [new(UnitTypeId, "Gramo", "g")];

    private static IReadOnlyList<RecipeTagResponse> Tags =>
        [new(TagOneId, "Vegano"), new(TagTwoId, "Rápido")];

    private static IReadOnlyList<MealTypeResponse> MealTypes =>
        [new(Guid.Parse("40000000-0000-0000-0000-000000000001"), "Comida", 1)];
}
