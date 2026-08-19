using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Exceptions;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Domain.Recipes;

namespace Friggy.Application.Recipes.Services;

/// <summary>
/// Servicio de aplicación que coordina la creación, consulta, actualización
/// y eliminación de recetas.
/// </summary>
public sealed class RecipeService(
    IRecipeRepository recipes,
    IRecipeCatalogRepository catalogs)
{
    /// <summary>
    /// Obtiene los resúmenes de las recetas ordenados por nombre.
    /// </summary>
    public async Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(
        CancellationToken cancellationToken) =>
        (await recipes.ListSummariesAsync(cancellationToken))
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    /// <summary>
    /// Obtiene una receta completa por su identificador.
    /// </summary>
    public async Task<RecipeResponse> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    /// <summary>
    /// Crea una receta, comprueba sus referencias y persiste sus cambios.
    /// </summary>
    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recipe = Build(
            request.Name,
            request.EstimatedMinutes,
            request.Ingredients,
            request.Steps,
            request.TagIds,
            request.MealTypeIds);

        await EnsureReferencesExistAsync(recipe, cancellationToken);
        await EnsureUniqueNameAsync(recipe.Name.Normalized, null, cancellationToken);
        await recipes.AddAsync(recipe, cancellationToken);
        await recipes.SaveChangesAsync(cancellationToken);
        return Map(recipe);
    }

    /// <summary>
    /// Actualiza una receta, comprueba sus referencias y persiste sus cambios.
    /// </summary>
    public async Task<RecipeResponse> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await FindAsync(id, cancellationToken);
        var replacement = Build(
            request.Name,
            request.EstimatedMinutes,
            request.Ingredients,
            request.Steps,
            request.TagIds,
            request.MealTypeIds);

        await EnsureReferencesExistAsync(replacement, cancellationToken);
        await EnsureUniqueNameAsync(replacement.Name.Normalized, existing.Id, cancellationToken);
        existing.ReplaceWith(replacement);
        await recipes.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    /// <summary>
    /// Elimina una receta y persiste sus cambios.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await FindAsync(id, cancellationToken);
        recipes.Remove(recipe);
        await recipes.SaveChangesAsync(cancellationToken);
    }

    private static Recipe Build(
        string? name,
        int estimatedMinutes,
        IReadOnlyList<RecipeIngredientRequest>? ingredientRequests,
        IReadOnlyList<RecipeStepRequest>? stepRequests,
        IReadOnlyList<Guid>? tagIds,
        IReadOnlyList<Guid>? mealTypeIds)
    {
        var recipe = Recipe.Create(name, TimeSpan.FromMinutes(estimatedMinutes));

        foreach (var item in (ingredientRequests ?? []).OrderBy(item => item.Order))
        {
            recipe.AddIngredient(
                item.IngredientId,
                item.UnitTypeId,
                item.Quantity,
                item.Order,
                item.Id);
        }

        foreach (var item in (stepRequests ?? []).OrderBy(item => item.Order))
        {
            var step = recipe.AddStep(
                item.Description,
                item.EstimatedMinutes.HasValue
                    ? TimeSpan.FromMinutes(item.EstimatedMinutes.Value)
                    : null,
                item.Order);

            foreach (var recipeIngredientId in item.RecipeIngredientIds ?? [])
            {
                recipe.AssignIngredientToStep(step.Id, recipeIngredientId);
            }
        }

        foreach (var tagId in tagIds ?? [])
        {
            recipe.AddTag(tagId);
        }

        foreach (var mealTypeId in mealTypeIds ?? [])
        {
            recipe.AddMealType(mealTypeId);
        }

        recipe.EnsureComplete();
        return recipe;
    }

    private async Task<Recipe> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await recipes.GetByIdAsync(id, cancellationToken) ??
        throw new RecipeNotFoundException(
            "recipe.not-found",
            "No se encontró la receta.");

    private async Task EnsureUniqueNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await recipes.ExistsByNormalizedNameAsync(
            normalizedName,
            excludingId,
            cancellationToken))
        {
            throw new RecipeNameConflictException(
                "recipe.name.duplicate",
                "Ya existe una receta con ese nombre.");
        }
    }

    private async Task EnsureReferencesExistAsync(
        Recipe recipe,
        CancellationToken cancellationToken)
    {
        var ingredientIds = recipe.Ingredients
            .Select(item => item.IngredientId)
            .Distinct()
            .ToArray();
        if (!await catalogs.IngredientsExistAsync(ingredientIds, cancellationToken))
        {
            throw new RecipeReferenceNotFoundException(
                "recipe.ingredient.not-found",
                "No se encontró uno de los ingredientes de la receta.");
        }

        var unitTypeIds = recipe.Ingredients
            .Select(item => item.UnitTypeId)
            .Distinct()
            .ToArray();
        if (!await catalogs.UnitTypesExistAsync(unitTypeIds, cancellationToken))
        {
            throw new RecipeReferenceNotFoundException(
                "recipe.unit-type.not-found",
                "No se encontró una de las unidades de la receta.");
        }

        if (!await catalogs.TagsExistAsync(recipe.TagIds, cancellationToken))
        {
            throw new RecipeReferenceNotFoundException(
                "recipe.tag.not-found",
                "No se encontró una de las etiquetas de la receta.");
        }

        if (!await catalogs.MealTypesExistAsync(recipe.MealTypeIds, cancellationToken))
        {
            throw new RecipeReferenceNotFoundException(
                "recipe.meal-type.not-found",
                "No se encontró uno de los tipos de comida de la receta.");
        }
    }

    private static RecipeResponse Map(Recipe recipe)
    {
        var ingredientOrderById = recipe.Ingredients.ToDictionary(
            item => item.Id,
            item => item.Order);

        return new(
            recipe.Id,
            recipe.Name.Value,
            ToMinutes(recipe.EstimatedTime),
            recipe.Ingredients
                .OrderBy(item => item.Order)
                .Select(item => new RecipeIngredientResponse(
                    item.Id,
                    item.IngredientId,
                    item.UnitTypeId,
                    item.Quantity,
                    item.Order))
                .ToArray(),
            recipe.Steps
                .OrderBy(item => item.Order)
                .Select(item => new RecipeStepResponse(
                    item.Id,
                    item.Description,
                    item.EstimatedTime.HasValue
                        ? ToMinutes(item.EstimatedTime.Value)
                        : null,
                    item.Order,
                    item.RecipeIngredientIds
                        .OrderBy(id => ingredientOrderById[id])
                        .ToArray()))
                .ToArray(),
            recipe.TagIds.ToArray(),
            recipe.MealTypeIds.ToArray());
    }

    private static int ToMinutes(TimeSpan value) => checked((int)value.TotalMinutes);
}
