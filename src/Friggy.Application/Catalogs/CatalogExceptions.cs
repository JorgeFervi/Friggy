using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs;

/// <summary>
/// Clase base para los errores que se producen al ejecutar casos de uso
/// relacionados con catálogos.
/// </summary>
public abstract class CatalogException(string code, string message) : Exception(message)
{
    /// <summary>
    /// Código que identifica la regla o el fallo de aplicación.
    /// </summary>
    public string Code { get; } = code;
}

/// <summary>
/// Excepción que indica un conflicto al modificar un catálogo.
/// </summary>
public sealed class CatalogConflictException(string code, string message)
    : CatalogException(code, message);

/// <summary>
/// Excepción que indica que no se encontró un elemento de catálogo.
/// </summary>
public sealed class CatalogNotFoundException(string code, string message)
    : CatalogException(code, message);

/// <summary>
/// Enumeración con los tipos de fallo que pueden producirse en un catálogo.
/// </summary>
public enum CatalogFailureKind
{
    /// <summary>
    /// Fallo causado por una validación de dominio.
    /// </summary>
    Validation,
    /// <summary>
    /// Fallo causado porque no se encontró un elemento.
    /// </summary>
    NotFound,
    /// <summary>
    /// Fallo causado por un conflicto de negocio.
    /// </summary>
    Conflict,
}

/// <summary>
/// Resultado clasificado de un fallo producido en un catálogo.
/// </summary>
public sealed record CatalogFailure(CatalogFailureKind Kind, string Code);

/// <summary>
/// Clase que traduce excepciones de dominio y aplicación a fallos de catálogo.
/// </summary>
public static class CatalogFailureClassifier
{
    /// <summary>
    /// Método que clasifica una excepción relacionada con un catálogo.
    /// </summary>
    /// <param name="exception">
    /// Excepción que se va a clasificar.
    /// </param>
    /// <returns>
    /// Fallo clasificado o <see langword="null"/> cuando la excepción no se
    /// reconoce como un fallo de catálogo.
    /// </returns>
    public static CatalogFailure? Classify(Exception exception) => exception switch
    {
        DomainValidationException validation => new(CatalogFailureKind.Validation, validation.Code),
        CatalogNotFoundException notFound => new(CatalogFailureKind.NotFound, notFound.Code),
        CatalogConflictException conflict => new(CatalogFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
