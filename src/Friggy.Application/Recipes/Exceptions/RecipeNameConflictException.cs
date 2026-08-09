namespace Friggy.Application.Recipes.Exceptions;

public sealed class RecipeNameConflictException(string code, string message)
    : RecipeApplicationException(code, message);
