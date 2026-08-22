namespace Friggy.Application.DailyPlans.Exceptions;

/// <summary>Base de los fallos de planificación diaria.</summary>
public abstract class DailyPlanApplicationException(string code, string message)
    : Exception(message)
{
    /// <summary>Código estable del fallo.</summary>
    public string Code { get; } = code;
}

/// <summary>Indica que no existe el plan solicitado.</summary>
public sealed class DailyPlanNotFoundException(string code, string message)
    : DailyPlanApplicationException(code, message);

/// <summary>Indica que ya existe un plan para la fecha.</summary>
public sealed class DailyPlanDateConflictException(string code, string message)
    : DailyPlanApplicationException(code, message);

/// <summary>Indica que falta una referencia requerida.</summary>
public sealed class DailyPlanReferenceNotFoundException(string code, string message)
    : DailyPlanApplicationException(code, message);

/// <summary>Tipo HTTP de un fallo diario.</summary>
public enum DailyPlanFailureKind
{
    /// <summary>Recurso inexistente.</summary>
    NotFound,

    /// <summary>Conflicto con el estado actual.</summary>
    Conflict,
}

/// <summary>Fallo diario clasificado.</summary>
public sealed record DailyPlanFailure(DailyPlanFailureKind Kind, string Code);

/// <summary>Clasifica excepciones conocidas de planificación diaria.</summary>
public static class DailyPlanFailureClassifier
{
    /// <summary>Clasifica una excepción o devuelve nulo.</summary>
    public static DailyPlanFailure? Classify(Exception exception) => exception switch
    {
        DailyPlanNotFoundException notFound =>
            new(DailyPlanFailureKind.NotFound, notFound.Code),
        DailyPlanReferenceNotFoundException reference =>
            new(DailyPlanFailureKind.NotFound, reference.Code),
        DailyPlanDateConflictException conflict =>
            new(DailyPlanFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
