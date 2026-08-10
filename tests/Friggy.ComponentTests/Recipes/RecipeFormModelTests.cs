using System.ComponentModel.DataAnnotations;
using Friggy.Web.Recipes;

namespace Friggy.ComponentTests.Recipes;

public sealed class RecipeFormModelTests
{
    private static readonly Guid IngredientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Category", "Component")]
    public void Validate_NoIngredientsOrSteps_ReturnsBothCollectionErrors()
    {
        var model = new RecipeFormModel();

        var results = Validate(model);

        Assert.Collection(
            results,
            result => AssertValidation(
                result,
                "Añade al menos un ingrediente.",
                nameof(RecipeFormModel.Ingredients)),
            result => AssertValidation(
                result,
                "Añade al menos un paso.",
                nameof(RecipeFormModel.Steps)));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Validate_InvalidIngredient_ReturnsAllIngredientErrors()
    {
        var model = new RecipeFormModel();
        model.Ingredients.Add(new(Guid.Empty, Guid.Empty, 0m, 0));
        model.Steps.Add(new("Servir", null, 0));

        var results = Validate(model);

        Assert.Collection(
            results,
            result => AssertValidation(
                result,
                "Selecciona un ingrediente.",
                nameof(RecipeFormModel.Ingredients)),
            result => AssertValidation(
                result,
                "Selecciona una unidad.",
                nameof(RecipeFormModel.Ingredients)),
            result => AssertValidation(
                result,
                "La cantidad de cada ingrediente debe ser mayor que cero.",
                nameof(RecipeFormModel.Ingredients)));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Validate_InvalidStep_ReturnsAllStepErrors()
    {
        var model = new RecipeFormModel();
        model.Ingredients.Add(new(IngredientId, UnitTypeId, 1m, 0));
        model.Steps.Add(new("   ", -1, 0));

        var results = Validate(model);

        Assert.Collection(
            results,
            result => AssertValidation(
                result,
                "La descripción de cada paso es obligatoria.",
                nameof(RecipeFormModel.Steps)),
            result => AssertValidation(
                result,
                "El tiempo de un paso no puede ser negativo.",
                nameof(RecipeFormModel.Steps)));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Validate_CompleteRecipe_ReturnsNoErrors()
    {
        var model = new RecipeFormModel();
        model.Ingredients.Add(new(IngredientId, UnitTypeId, 1m, 0));
        model.Steps.Add(new("Servir", null, 0));

        var results = Validate(model);

        Assert.Empty(results);
    }

    private static ValidationResult[] Validate(RecipeFormModel model) =>
        model.Validate(new ValidationContext(model)).ToArray();

    private static void AssertValidation(
        ValidationResult result,
        string expectedMessage,
        string expectedMember)
    {
        Assert.Equal(expectedMessage, result.ErrorMessage);
        Assert.Equal([expectedMember], result.MemberNames);
    }
}
