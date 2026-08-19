namespace Friggy.Application.Recipes.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró una receta.
/// </summary>
public sealed class RecipeNotFoundException(string code, string message)
    : RecipeApplicationException(code, message);
