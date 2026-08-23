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
    private static readonly Guid LineOneId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid LineTwoId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly Guid TagOneId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid TagTwoId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    [Fact]
    [Trait("Category", "Component")]
    public void IngredientUnitSelector_FiltersCookingUnitsButKeepsHistoricalSelection()
    {
        var disabledId = Guid.NewGuid();
        var disabled = new UnitTypeResponse(disabledId, "Cucharada retirada", "cda")
        {
            MeasurementDimension = "volume",
            BaseUnitFactor = 15m,
            CanUseForCooking = false,
            CanUseForShopping = false,
        };
        var enabled = new UnitTypeResponse(UnitTypeId, "Gramo", "g");
        var newLine = new RecipeIngredientFormModel();
        var persistedLine = new RecipeIngredientFormModel(
            IngredientOneId, disabledId, 1m, 0, Guid.NewGuid());

        var newRow = Render<RecipeIngredientRow>(parameters => parameters
            .Add(item => item.Model, newLine)
            .Add(item => item.Ingredients, Ingredients)
            .Add(item => item.UnitTypes, [enabled, disabled])
            .Add(item => item.IsExpanded, true));
        var historicalRow = Render<RecipeIngredientRow>(parameters => parameters
            .Add(item => item.Model, persistedLine)
            .Add(item => item.Ingredients, Ingredients)
            .Add(item => item.UnitTypes, [enabled, disabled])
            .Add(item => item.IsExpanded, true));

        Assert.DoesNotContain(disabledId.ToString(), newRow.Find("select[data-field='unit-type']").InnerHtml);
        Assert.Contains(disabledId.ToString(), historicalRow.Find("select[data-field='unit-type']").InnerHtml);
        Assert.Contains("no disponible", historicalRow.Markup, StringComparison.Ordinal);
    }

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
    public void ExistingIngredients_RenderAsCompactSummaries()
    {
        var model = ValidModel();
        model.Ingredients.Add(new(IngredientOneId, UnitTypeId, 1.5m, 0));
        model.Ingredients.Add(new(IngredientTwoId, UnitTypeId, 2m, 1));

        var component = RenderForm(model);

        Assert.Contains("Ingredientes (2)", component.Markup, StringComparison.Ordinal);
        Assert.Empty(component.FindAll("[data-testid='ingredient-row'] select"));
        Assert.Contains("Tomate", component.FindAll("[data-testid='ingredient-row']")[0].TextContent);
        Assert.Contains("1,50 Gramo", component.FindAll("[data-testid='ingredient-row']")[0].TextContent);
        Assert.Contains("Pepino", component.FindAll("[data-testid='ingredient-row']")[1].TextContent);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void EditIngredient_AnotherRowSelected_ExpandsOnlySelectedRow()
    {
        var model = ValidModel();
        model.Ingredients.Add(new(IngredientOneId, UnitTypeId, 1m, 0));
        model.Ingredients.Add(new(IngredientTwoId, UnitTypeId, 2m, 1));
        var component = RenderForm(model);

        component.Find("button[aria-label='Editar ingrediente 1']").Click();
        Assert.Single(component.FindAll("[data-testid='ingredient-row'] select[data-field='ingredient']"));

        component.Find("button[aria-label='Editar ingrediente 2']").Click();

        var editor = Assert.Single(component.FindAll("[data-testid='ingredient-editor']"));
        Assert.Equal(
            IngredientTwoId.ToString(),
            editor.QuerySelector("select[data-field='ingredient']")?.GetAttribute("value"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void AddIngredient_NewRow_ExpandsItAndCollapsesPreviousEditor()
    {
        var model = ValidModel();
        model.Ingredients.Add(new(IngredientOneId, UnitTypeId, 1m, 0));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar ingrediente 1']").Click();

        component.Find("button[data-action='add-ingredient']").Click();

        Assert.Equal(2, model.Ingredients.Count);
        var editor = Assert.Single(component.FindAll("[data-testid='ingredient-editor']"));
        Assert.Equal(
            Guid.Empty.ToString(),
            editor.QuerySelector("select[data-field='ingredient']")?.GetAttribute("value"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FinishIngredient_AfterChangingValues_CollapsesAndPreservesSummary()
    {
        var model = ValidModel();
        model.Ingredients.Add(new(IngredientOneId, UnitTypeId, 1m, 0));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar ingrediente 1']").Click();

        component.Find("[data-testid='ingredient-editor'] input[data-field='quantity']").Change("2.5");
        component.Find("button[aria-label='Terminar edición del ingrediente 1']").Click();

        Assert.Empty(component.FindAll("[data-testid='ingredient-editor']"));
        Assert.Contains("2,50 Gramo", component.Find("[data-testid='ingredient-row']").TextContent);
        Assert.Equal(2.5m, model.Ingredients[0].Quantity);
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

        component.Find("button[aria-label='Editar paso 1']").Click();
        component.Find("button[aria-label='Mover paso 2 arriba']").Click();

        Assert.Equal([second.ClientId, first.ClientId], model.Steps.Select(item => item.ClientId));
        Assert.Equal([0, 1], model.Steps.Select(item => item.Order));
        Assert.Equal(
            ["Servir", "Triturar"],
            component.FindAll("[data-testid='step-row']")
                .Select(element => element.TextContent.Contains("Servir", StringComparison.Ordinal) ? "Servir" : "Triturar"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ExistingSteps_RenderAsCompactSummaries()
    {
        var model = ValidModel();
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        model.Steps.Add(new RecipeStepFormModel("Servir", 5, 1));

        var component = RenderForm(model);

        Assert.Contains("Pasos (2)", component.Markup, StringComparison.Ordinal);
        Assert.Empty(component.FindAll("[data-testid='step-row'] textarea"));
        Assert.Contains("Triturar", component.FindAll("[data-testid='step-row']")[0].TextContent);
        Assert.Contains("Servir", component.FindAll("[data-testid='step-row']")[1].TextContent);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void EditStep_AnotherRowSelected_ExpandsOnlySelectedRow()
    {
        var model = ValidModel();
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        model.Steps.Add(new RecipeStepFormModel("Servir", 5, 1));
        var component = RenderForm(model);

        component.Find("button[aria-label='Editar paso 1']").Click();
        Assert.Single(component.FindAll("[data-testid='step-row'] textarea"));

        component.Find("button[aria-label='Editar paso 2']").Click();

        var editor = Assert.Single(component.FindAll("[data-testid='step-editor']"));
        Assert.Equal("Servir", editor.QuerySelector("textarea")?.GetAttribute("value"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void AddStep_NewRow_ExpandsItAndCollapsesPreviousEditor()
    {
        var model = ValidModel();
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar paso 1']").Click();

        component.Find("button[data-action='add-step']").Click();

        Assert.Equal(2, model.Steps.Count);
        Assert.Single(component.FindAll("[data-testid='step-editor']"));
        Assert.Contains("Paso 2", component.Find("[data-testid='step-editor']").TextContent);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void StepIngredients_UsesIngredientsHeadingWithoutLinePrefix()
    {
        var model = ValidModel();
        model.Ingredients.Add(new RecipeIngredientFormModel(IngredientOneId, UnitTypeId, 1m, 0));
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar paso 1']").Click();

        var ingredients = component.Find("[data-testid='step-ingredients']");

        Assert.Contains("Ingredientes", ingredients.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Ingredientes utilizados", ingredients.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Línea 1:", ingredients.TextContent, StringComparison.Ordinal);
        Assert.Contains("1 Gramo (g) de Tomate", ingredients.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void SelectStepIngredient_ThenReorderLines_PreservesSelectionByIdentity()
    {
        var model = ValidModel();
        model.Ingredients.Add(
            new RecipeIngredientFormModel(IngredientOneId, UnitTypeId, 1m, 0, LineOneId));
        model.Ingredients.Add(
            new RecipeIngredientFormModel(IngredientTwoId, UnitTypeId, 2m, 1, LineTwoId));
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar paso 1']").Click();

        component.Find($"input[data-recipe-ingredient-id='{LineOneId}']").Change(true);
        component.Find("button[aria-label='Mover ingrediente 2 arriba']").Click();

        Assert.Equal([LineOneId], Assert.Single(model.Steps).RecipeIngredientIds);
        Assert.Equal(
            [LineOneId],
            Assert.Single(model.ToUpdateRequest().Steps).RecipeIngredientIds);
        Assert.True(
            component.Find($"input[data-recipe-ingredient-id='{LineOneId}']")
                .HasAttribute("checked"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void RemoveIngredient_AssociatedLine_RemovesSelectionAndUpdatesStepOptions()
    {
        var model = ValidModel();
        model.Ingredients.Add(
            new RecipeIngredientFormModel(IngredientOneId, UnitTypeId, 1m, 0, LineOneId));
        model.Ingredients.Add(
            new RecipeIngredientFormModel(IngredientTwoId, UnitTypeId, 2m, 1, LineTwoId));
        model.Steps.Add(
            new RecipeStepFormModel("Triturar", null, 0, [LineOneId, LineTwoId]));
        var component = RenderForm(model);
        component.Find("button[aria-label='Editar paso 1']").Click();

        component.Find("button[aria-label='Eliminar ingrediente 1']").Click();

        Assert.Equal([LineTwoId], Assert.Single(model.Steps).RecipeIngredientIds);
        Assert.Empty(component.FindAll($"input[data-recipe-ingredient-id='{LineOneId}']"));
        Assert.Single(component.FindAll($"input[data-recipe-ingredient-id='{LineTwoId}']"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void StepWithNoIngredientLines_RendersAccessibleEmptyState()
    {
        var model = ValidModel();
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));

        var component = RenderForm(model);
        component.Find("button[aria-label='Editar paso 1']").Click();

        var associations = component.Find("[data-testid='step-ingredients']");
        Assert.Contains(
            "Añade ingredientes a la receta para poder asociarlos",
            associations.TextContent,
            StringComparison.Ordinal);
        Assert.Empty(associations.QuerySelectorAll("input[type='checkbox']"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void AddIngredient_AfterStepExists_UpdatesAvailableAssociationsImmediately()
    {
        var model = ValidModel();
        model.Steps.Add(new RecipeStepFormModel("Triturar", null, 0));
        var component = RenderForm(model);

        component.Find("button[data-action='add-ingredient']").Click();
        component.Find("button[aria-label='Editar paso 1']").Click();

        var ingredient = Assert.Single(model.Ingredients);
        Assert.Single(
            component.FindAll($"input[data-recipe-ingredient-id='{ingredient.Id}']"));
        Assert.DoesNotContain(
            "Añade ingredientes a la receta para poder asociarlos",
            component.Find("[data-testid='step-ingredients']").TextContent,
            StringComparison.Ordinal);
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
