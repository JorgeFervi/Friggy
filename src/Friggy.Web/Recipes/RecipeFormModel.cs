using System.ComponentModel.DataAnnotations;
using Friggy.Application.Recipes.Dtos;

namespace Friggy.Web.Recipes;

public sealed class RecipeFormModel : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(160, ErrorMessage = "El nombre no puede superar 160 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "El tiempo estimado debe ser mayor que cero.")]
    public int EstimatedMinutes { get; set; }

    public List<RecipeIngredientFormModel> Ingredients { get; } = [];
    public List<RecipeStepFormModel> Steps { get; } = [];
    public HashSet<Guid> TagIds { get; } = [];
    public HashSet<Guid> MealTypeIds { get; } = [];

    public static RecipeFormModel FromResponse(RecipeResponse response)
    {
        var model = new RecipeFormModel
        {
            Name = response.Name,
            EstimatedMinutes = response.EstimatedMinutes,
        };

        model.Ingredients.AddRange(response.Ingredients
            .OrderBy(item => item.Order)
            .Select(item => new RecipeIngredientFormModel(
                item.IngredientId,
                item.UnitTypeId,
                item.Quantity,
                item.Order,
                item.Id)));
        model.Steps.AddRange(response.Steps
            .OrderBy(item => item.Order)
            .Select(item => new RecipeStepFormModel(
                item.Description,
                item.EstimatedMinutes,
                item.Order,
                item.RecipeIngredientIds)));
        model.TagIds.UnionWith(response.TagIds);
        model.MealTypeIds.UnionWith(response.MealTypeIds);
        return model;
    }

    public CreateRecipeRequest ToCreateRequest() =>
        new(
            Name,
            EstimatedMinutes,
            MapIngredients(),
            MapSteps(),
            TagIds.Order().ToArray(),
            MealTypeIds.Order().ToArray());

    public UpdateRecipeRequest ToUpdateRequest() =>
        new(
            Name,
            EstimatedMinutes,
            MapIngredients(),
            MapSteps(),
            TagIds.Order().ToArray(),
            MealTypeIds.Order().ToArray());

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Ingredients.Count == 0)
        {
            yield return new ValidationResult(
                "Añade al menos un ingrediente.",
                [nameof(Ingredients)]);
        }

        foreach (var ingredient in Ingredients)
        {
            if (ingredient.IngredientId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Selecciona un ingrediente.",
                    [nameof(Ingredients)]);
            }

            if (ingredient.UnitTypeId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Selecciona una unidad.",
                    [nameof(Ingredients)]);
            }

            if (ingredient.Quantity <= 0)
            {
                yield return new ValidationResult(
                    "La cantidad de cada ingrediente debe ser mayor que cero.",
                    [nameof(Ingredients)]);
            }
        }

        if (Steps.Count == 0)
        {
            yield return new ValidationResult(
                "Añade al menos un paso.",
                [nameof(Steps)]);
        }

        foreach (var step in Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Description))
            {
                yield return new ValidationResult(
                    "La descripción de cada paso es obligatoria.",
                    [nameof(Steps)]);
            }

            if (step.EstimatedMinutes < 0)
            {
                yield return new ValidationResult(
                    "El tiempo de un paso no puede ser negativo.",
                    [nameof(Steps)]);
            }
        }
    }

    private RecipeIngredientRequest[] MapIngredients() =>
        Ingredients
            .Select((item, order) => new RecipeIngredientRequest(
                item.IngredientId,
                item.UnitTypeId,
                item.Quantity,
                order,
                item.Id))
            .ToArray();

    private RecipeStepRequest[] MapSteps() =>
        Steps
            .Select((item, order) => new RecipeStepRequest(
                item.Description,
                item.EstimatedMinutes,
                order,
                item.RecipeIngredientIds
                    .Where(id => Ingredients.Any(ingredient => ingredient.Id == id))
                    .OrderBy(id => Ingredients.FindIndex(ingredient => ingredient.Id == id))
                    .ToArray()))
            .ToArray();
}
