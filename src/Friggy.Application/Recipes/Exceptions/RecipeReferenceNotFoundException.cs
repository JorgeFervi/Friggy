namespace Friggy.Application.Recipes.Exceptions;

public sealed class RecipeReferenceNotFoundException(string code, string message)
    : RecipeApplicationException(code, message);
