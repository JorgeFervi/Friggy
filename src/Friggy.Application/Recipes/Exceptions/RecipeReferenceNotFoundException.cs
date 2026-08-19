namespace Friggy.Application.Recipes.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró una referencia necesaria para
/// construir o modificar una receta.
/// </summary>
public sealed class RecipeReferenceNotFoundException(string code, string message)
    : RecipeApplicationException(code, message);
