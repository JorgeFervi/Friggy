using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs;

public abstract class CatalogException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class CatalogConflictException(string code, string message)
    : CatalogException(code, message);

public sealed class CatalogNotFoundException(string code, string message)
    : CatalogException(code, message);

public enum CatalogFailureKind
{
    Validation,
    NotFound,
    Conflict,
}

public sealed record CatalogFailure(CatalogFailureKind Kind, string Code);

public static class CatalogFailureClassifier
{
    public static CatalogFailure? Classify(Exception exception) => exception switch
    {
        DomainValidationException validation => new(CatalogFailureKind.Validation, validation.Code),
        CatalogNotFoundException notFound => new(CatalogFailureKind.NotFound, notFound.Code),
        CatalogConflictException conflict => new(CatalogFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
