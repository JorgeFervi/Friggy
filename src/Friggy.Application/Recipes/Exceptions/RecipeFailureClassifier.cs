using Friggy.Domain.Recipes;

namespace Friggy.Application.Recipes.Exceptions;

public enum RecipeFailureKind
{
    NotFound,
    Conflict,
}

public sealed record RecipeFailure(RecipeFailureKind Kind, string Code);

public static class RecipeFailureClassifier
{
    public static RecipeFailure? Classify(Exception exception) => exception switch
    {
        RecipeNotFoundException notFound =>
            new(RecipeFailureKind.NotFound, notFound.Code),
        RecipeReferenceNotFoundException reference =>
            new(RecipeFailureKind.NotFound, reference.Code),
        RecipeNameConflictException conflict =>
            new(RecipeFailureKind.Conflict, conflict.Code),
        RecipeConflictException conflict =>
            new(RecipeFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
