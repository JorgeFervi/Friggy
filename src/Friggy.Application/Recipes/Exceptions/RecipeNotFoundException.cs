namespace Friggy.Application.Recipes.Exceptions;

public sealed class RecipeNotFoundException(string code, string message)
    : RecipeApplicationException(code, message);
