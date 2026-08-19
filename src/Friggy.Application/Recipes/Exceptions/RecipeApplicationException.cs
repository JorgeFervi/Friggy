namespace Friggy.Application.Recipes.Exceptions;

/// <summary>
/// Clase base para los errores producidos al ejecutar casos de uso de recetas.
/// </summary>
public abstract class RecipeApplicationException(string code, string message)
    : Exception(message)
{
    /// <summary>
    /// Código que identifica el fallo de aplicación.
    /// </summary>
    public string Code { get; } = code;
}
