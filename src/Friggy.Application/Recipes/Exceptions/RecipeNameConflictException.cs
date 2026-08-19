namespace Friggy.Application.Recipes.Exceptions;

/// <summary>
/// Excepción que indica un conflicto con el nombre de una receta.
/// </summary>
public sealed class RecipeNameConflictException(string code, string message)
    : RecipeApplicationException(code, message);
