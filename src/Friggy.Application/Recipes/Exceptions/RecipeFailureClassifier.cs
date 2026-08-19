using Friggy.Domain.Recipes;

namespace Friggy.Application.Recipes.Exceptions;

/// <summary>
/// Enumeración con los tipos de fallo que pueden producirse en recetas.
/// </summary>
public enum RecipeFailureKind
{
    /// <summary>
    /// Fallo causado porque no se encontró un recurso.
    /// </summary>
    NotFound,
    /// <summary>
    /// Fallo causado por un conflicto de negocio.
    /// </summary>
    Conflict,
}

/// <summary>
/// Resultado clasificado de un fallo producido en una receta.
/// </summary>
public sealed record RecipeFailure(RecipeFailureKind Kind, string Code);

/// <summary>
/// Clase que traduce excepciones de recetas y del dominio a fallos de aplicación.
/// </summary>
public static class RecipeFailureClassifier
{
    /// <summary>
    /// Método que clasifica una excepción relacionada con recetas.
    /// </summary>
    /// <param name="exception">
    /// Excepción que se va a clasificar.
    /// </param>
    /// <returns>
    /// Fallo clasificado o <see langword="null"/> cuando la excepción no se
    /// reconoce como un fallo de receta.
    /// </returns>
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
