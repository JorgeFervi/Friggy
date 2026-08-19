using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="Recipe"/> que representa una receta compuesta por
/// ingredientes, pasos, etiquetas y tipos de comida.
/// </summary>
public sealed class Recipe
{
    private readonly List<RecipeIngredient> ingredients = [];
    private readonly List<RecipeStep> steps = [];
    private readonly List<RecipeTagLink> tags = [];
    private readonly List<RecipeMealTypeLink> mealTypes = [];

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private Recipe()
    {
        Name = null!;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar a la receta de forma interna.
    /// </param>
    /// <param name="name">
    /// Nombre de la receta.
    /// </param>
    /// <param name="estimatedTime">
    /// Tiempo estimado de preparación.
    /// </param>
    private Recipe(Guid id, CatalogName name, TimeSpan estimatedTime)
    {
        Id = id;
        Name = name;
        EstimatedTime = estimatedTime;
    }

    /// <summary>
    /// Código único para identificar a la receta de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre de la receta.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Tiempo estimado de preparación de la receta.
    /// </summary>
    public TimeSpan EstimatedTime { get; private set; }

    /// <summary>
    /// Líneas de ingredientes de la receta.
    /// </summary>
    public IReadOnlyList<RecipeIngredient> Ingredients => ingredients.AsReadOnly();

    /// <summary>
    /// Pasos de preparación de la receta.
    /// </summary>
    public IReadOnlyList<RecipeStep> Steps => steps.AsReadOnly();

    /// <summary>
    /// Etiquetas asociadas a la receta.
    /// </summary>
    public IReadOnlyList<RecipeTagLink> Tags => tags.AsReadOnly();

    /// <summary>
    /// Tipos de comida asociados a la receta.
    /// </summary>
    public IReadOnlyList<RecipeMealTypeLink> MealTypes => mealTypes.AsReadOnly();

    /// <summary>
    /// Códigos de las etiquetas asociadas a la receta.
    /// </summary>
    public IReadOnlyList<Guid> TagIds => tags.Select(item => item.RecipeTagId).ToArray();

    /// <summary>
    /// Códigos de los tipos de comida asociados a la receta.
    /// </summary>
    public IReadOnlyList<Guid> MealTypeIds => mealTypes.Select(item => item.MealTypeId).ToArray();

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre de la receta.
    /// </param>
    /// <param name="estimatedTime">
    /// Tiempo estimado de preparación.
    /// </param>
    /// <returns>
    /// Objeto <see cref="Recipe"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el nombre está vacío o el tiempo
    /// estimado es negativo.
    /// </exception>
    public static Recipe Create(string? name, TimeSpan estimatedTime)
    {
        ValidateEstimatedTime(estimatedTime);
        return new Recipe(
            Guid.NewGuid(),
            CatalogName.Create(name, "recipe.name.required"),
            estimatedTime);
    }

    /// <summary>
    /// Método para añadir una línea de ingrediente a la receta.
    /// </summary>
    /// <param name="ingredientId">
    /// Código del ingrediente.
    /// </param>
    /// <param name="unitTypeId">
    /// Código de la unidad de medida.
    /// </param>
    /// <param name="quantity">
    /// Cantidad del ingrediente.
    /// </param>
    /// <param name="order">
    /// Posición de la línea dentro de la receta.
    /// </param>
    /// <param name="id">
    /// Código opcional de la línea de ingrediente.
    /// </param>
    /// <returns>
    /// Línea de ingrediente añadida a la receta.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando algún valor no es válido.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando la identidad o la posición ya
    /// están siendo usadas por otra línea.
    /// </exception>
    public RecipeIngredient AddIngredient(
        Guid ingredientId,
        Guid unitTypeId,
        decimal quantity,
        int order,
        Guid? id = null)
    {
        var ingredient = RecipeIngredient.Create(
            Id,
            ingredientId,
            unitTypeId,
            quantity,
            order,
            id);

        if (ingredients.Any(item => item.Id == ingredient.Id))
        {
            throw new RecipeConflictException(
                "recipe-ingredient.id.duplicate",
                "No puede haber dos líneas de ingrediente con la misma identidad.");
        }

        if (ingredients.Any(item => item.Order == order))
        {
            throw new RecipeConflictException(
                "recipe-ingredient.order.duplicate",
                "No puede haber dos ingredientes en la misma posición.");
        }

        ingredients.Add(ingredient);
        return ingredient;
    }

    /// <summary>
    /// Método para retirar una línea de ingrediente de la receta y de sus pasos.
    /// </summary>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la línea existía y se retiró; en caso contrario,
    /// <see langword="false"/>.
    /// </returns>
    public bool RemoveIngredient(Guid recipeIngredientId)
    {
        var ingredient = ingredients.SingleOrDefault(item => item.Id == recipeIngredientId);
        if (ingredient is null)
        {
            return false;
        }

        foreach (var step in steps)
        {
            step.RemoveIngredient(recipeIngredientId);
        }

        return ingredients.Remove(ingredient);
    }

    /// <summary>
    /// Método para añadir un paso de preparación a la receta.
    /// </summary>
    /// <param name="description">
    /// Descripción del paso.
    /// </param>
    /// <param name="estimatedTime">
    /// Tiempo estimado del paso.
    /// </param>
    /// <param name="order">
    /// Posición del paso dentro de la receta.
    /// </param>
    /// <returns>
    /// Paso añadido a la receta.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando algún valor no es válido.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando la posición ya está siendo usada
    /// por otro paso.
    /// </exception>
    public RecipeStep AddStep(string? description, TimeSpan? estimatedTime, int order)
    {
        var step = RecipeStep.Create(Id, description, estimatedTime, order);

        if (steps.Any(item => item.Order == order))
        {
            throw new RecipeConflictException(
                "recipe-step.order.duplicate",
                "No puede haber dos pasos en la misma posición.");
        }

        steps.Add(step);
        return step;
    }

    /// <summary>
    /// Método para retirar un paso de preparación de la receta.
    /// </summary>
    /// <param name="recipeStepId">
    /// Código del paso que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si el paso existía y se retiró; en caso contrario,
    /// <see langword="false"/>.
    /// </returns>
    public bool RemoveStep(Guid recipeStepId)
    {
        var step = steps.SingleOrDefault(item => item.Id == recipeStepId);
        if (step is null)
        {
            return false;
        }

        step.ClearIngredientAssociations();
        return steps.Remove(step);
    }

    /// <summary>
    /// Método para asociar una línea de ingrediente a un paso de la receta.
    /// </summary>
    /// <param name="recipeStepId">
    /// Código del paso de preparación.
    /// </param>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando alguno de los identificadores no
    /// existe en la receta.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando la línea ya está asociada al paso.
    /// </exception>
    public void AssignIngredientToStep(Guid recipeStepId, Guid recipeIngredientId)
    {
        var step = GetStep(recipeStepId);
        var ingredient = GetIngredient(recipeIngredientId);
        step.AssignIngredient(ingredient);
    }

    /// <summary>
    /// Método para retirar la asociación entre una línea de ingrediente y un paso.
    /// </summary>
    /// <param name="recipeStepId">
    /// Código del paso de preparación.
    /// </param>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la asociación existía y se retiró; en caso
    /// contrario, <see langword="false"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando alguno de los identificadores no
    /// existe en la receta.
    /// </exception>
    public bool RemoveIngredientFromStep(Guid recipeStepId, Guid recipeIngredientId)
    {
        var step = GetStep(recipeStepId);
        _ = GetIngredient(recipeIngredientId);
        return step.RemoveIngredient(recipeIngredientId);
    }

    /// <summary>
    /// Método para asociar una etiqueta a la receta.
    /// </summary>
    /// <param name="recipeTagId">
    /// Código de la etiqueta.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando la etiqueta ya está asociada.
    /// </exception>
    public void AddTag(Guid recipeTagId)
    {
        ValidateRequiredId(recipeTagId, "recipe.tag-id.required");
        if (tags.Any(item => item.RecipeTagId == recipeTagId))
        {
            throw new RecipeConflictException(
                "recipe.tag.duplicate",
                "La etiqueta ya está asignada a la receta.");
        }

        tags.Add(RecipeTagLink.Create(Id, recipeTagId));
    }

    /// <summary>
    /// Método para retirar una etiqueta de la receta.
    /// </summary>
    /// <param name="recipeTagId">
    /// Código de la etiqueta que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la etiqueta existía y se retiró; en caso
    /// contrario, <see langword="false"/>.
    /// </returns>
    public bool RemoveTag(Guid recipeTagId)
    {
        var tag = tags.SingleOrDefault(item => item.RecipeTagId == recipeTagId);
        return tag is not null && tags.Remove(tag);
    }

    /// <summary>
    /// Método para asociar un tipo de comida a la receta.
    /// </summary>
    /// <param name="mealTypeId">
    /// Código del tipo de comida.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío.
    /// </exception>
    /// <exception cref="RecipeConflictException">
    /// Excepción de dominio lanzada cuando el tipo de comida ya está asociado.
    /// </exception>
    public void AddMealType(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "recipe.meal-type-id.required");
        if (mealTypes.Any(item => item.MealTypeId == mealTypeId))
        {
            throw new RecipeConflictException(
                "recipe.meal-type.duplicate",
                "El tipo de comida ya está asignado a la receta.");
        }

        mealTypes.Add(RecipeMealTypeLink.Create(Id, mealTypeId));
    }

    /// <summary>
    /// Método para retirar un tipo de comida de la receta.
    /// </summary>
    /// <param name="mealTypeId">
    /// Código del tipo de comida que se va a retirar.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si el tipo de comida existía y se retiró; en caso
    /// contrario, <see langword="false"/>.
    /// </returns>
    public bool RemoveMealType(Guid mealTypeId)
    {
        var mealType = mealTypes.SingleOrDefault(item => item.MealTypeId == mealTypeId);
        return mealType is not null && mealTypes.Remove(mealType);
    }

    /// <summary>
    /// Método que comprueba que la receta contiene todos los elementos necesarios.
    /// </summary>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la receta no contiene ingredientes
    /// o pasos.
    /// </exception>
    public void EnsureComplete()
    {
        if (ingredients.Count == 0)
        {
            throw new DomainValidationException(
                "recipe.ingredients.required",
                "La receta debe contener al menos un ingrediente.");
        }

        if (steps.Count == 0)
        {
            throw new DomainValidationException(
                "recipe.steps.required",
                "La receta debe contener al menos un paso.");
        }
    }

    /// <summary>
    /// Método que reemplaza el contenido de la receta por el de otra receta.
    /// </summary>
    /// <param name="replacement">
    /// Receta cuyos datos se van a aplicar.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Excepción lanzada cuando la receta de reemplazo es nula.
    /// </exception>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando la receta de reemplazo no está
    /// completa o contiene valores no válidos.
    /// </exception>
    public void ReplaceWith(Recipe replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        replacement.EnsureComplete();

        var replacementName = CatalogName.Create(
            replacement.Name.Value,
            "recipe.name.required");
        ValidateEstimatedTime(replacement.EstimatedTime);

        var existingIngredientsById = ingredients.ToDictionary(item => item.Id);
        var replacementIngredients = replacement.Ingredients
            .Select(item => existingIngredientsById.TryGetValue(item.Id, out var existing)
                ? existing
                : RecipeIngredient.Create(
                    Id,
                    item.IngredientId,
                    item.UnitTypeId,
                    item.Quantity,
                    item.Order,
                    item.Id))
            .ToArray();
        var replacementSteps = replacement.Steps
            .Select(item => RecipeStep.Create(
                Id,
                item.Description,
                item.EstimatedTime,
                item.Order,
                item.Id))
            .ToArray();
        var replacementTags = replacement.TagIds
            .Select(tagId => RecipeTagLink.Create(Id, tagId))
            .ToArray();
        var replacementMealTypes = replacement.MealTypeIds
            .Select(mealTypeId => RecipeMealTypeLink.Create(Id, mealTypeId))
            .ToArray();

        var ingredientsById = replacementIngredients.ToDictionary(item => item.Id);
        var stepsById = replacementSteps.ToDictionary(item => item.Id);
        foreach (var sourceStep in replacement.Steps)
        {
            var targetStep = stepsById[sourceStep.Id];
            foreach (var recipeIngredientId in sourceStep.RecipeIngredientIds)
            {
                targetStep.AssignIngredient(ingredientsById[recipeIngredientId]);
            }
        }

        foreach (var sourceIngredient in replacement.Ingredients)
        {
            if (existingIngredientsById.TryGetValue(sourceIngredient.Id, out var existing))
            {
                existing.UpdateFrom(sourceIngredient);
            }
        }

        Name = replacementName;
        EstimatedTime = replacement.EstimatedTime;
        ingredients.Clear();
        ingredients.AddRange(replacementIngredients);
        steps.Clear();
        steps.AddRange(replacementSteps);
        tags.Clear();
        tags.AddRange(replacementTags);
        mealTypes.Clear();
        mealTypes.AddRange(replacementMealTypes);
    }

    /// <summary>
    /// Método que comprueba que el tiempo estimado no sea negativo.
    /// </summary>
    /// <param name="estimatedTime">
    /// Tiempo que se va a validar.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el tiempo es negativo.
    /// </exception>
    private static void ValidateEstimatedTime(TimeSpan estimatedTime)
    {
        if (estimatedTime < TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "recipe.estimated-time.non-negative",
                "El tiempo estimado no puede ser negativo.");
        }
    }

    /// <summary>
    /// Método que obtiene una línea de ingrediente de la receta.
    /// </summary>
    /// <param name="recipeIngredientId">
    /// Código de la línea de ingrediente que se va a obtener.
    /// </param>
    /// <returns>
    /// Línea de ingrediente encontrada.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío o no
    /// existe en la receta.
    /// </exception>
    private RecipeIngredient GetIngredient(Guid recipeIngredientId)
    {
        ValidateRequiredId(recipeIngredientId, "recipe-ingredient.id.required");
        return ingredients.SingleOrDefault(item => item.Id == recipeIngredientId) ??
            throw new DomainValidationException(
                "recipe-ingredient.not-found",
                "No se encontró la línea de ingrediente en la receta.");
    }

    /// <summary>
    /// Método que obtiene un paso de la receta.
    /// </summary>
    /// <param name="recipeStepId">
    /// Código del paso que se va a obtener.
    /// </param>
    /// <returns>
    /// Paso encontrado.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío o no
    /// existe en la receta.
    /// </exception>
    private RecipeStep GetStep(Guid recipeStepId)
    {
        ValidateRequiredId(recipeStepId, "recipe-step.id.required");
        return steps.SingleOrDefault(item => item.Id == recipeStepId) ??
            throw new DomainValidationException(
                "recipe-step.not-found",
                "No se encontró el paso en la receta.");
    }

    /// <summary>
    /// Método que comprueba que un identificador sea obligatorio.
    /// </summary>
    /// <param name="id">
    /// Identificador que se va a validar.
    /// </param>
    /// <param name="code">
    /// Código de error que se lanzará si el identificador no es válido.
    /// </param>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el identificador está vacío.
    /// </exception>
    private static void ValidateRequiredId(Guid id, string code)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException(code, "El identificador es obligatorio.");
        }
    }
}
