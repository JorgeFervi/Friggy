namespace Friggy.Application.Recipes.Exceptions;

public abstract class RecipeApplicationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
