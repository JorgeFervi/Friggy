namespace Friggy.Domain.Recipes;

public sealed class RecipeConflictException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
