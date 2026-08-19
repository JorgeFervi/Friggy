namespace Friggy.Application.WeeklyPlans.Exceptions;

/// <summary>
/// Enumeración con los tipos de fallo que pueden producirse en planes semanales.
/// </summary>
public enum WeeklyPlanFailureKind
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
/// Resultado clasificado de un fallo producido en un plan semanal.
/// </summary>
public sealed record WeeklyPlanFailure(WeeklyPlanFailureKind Kind, string Code);

/// <summary>
/// Clase que traduce excepciones de planificación a fallos de aplicación.
/// </summary>
public static class WeeklyPlanFailureClassifier
{
    /// <summary>
    /// Método que clasifica una excepción relacionada con un plan semanal.
    /// </summary>
    /// <param name="exception">
    /// Excepción que se va a clasificar.
    /// </param>
    /// <returns>
    /// Fallo clasificado o <see langword="null"/> cuando la excepción no se
    /// reconoce como un fallo de planificación.
    /// </returns>
    public static WeeklyPlanFailure? Classify(Exception exception) => exception switch
    {
        WeeklyPlanNotFoundException notFound =>
            new(WeeklyPlanFailureKind.NotFound, notFound.Code),
        WeeklyPlanReferenceNotFoundException reference =>
            new(WeeklyPlanFailureKind.NotFound, reference.Code),
        WeeklyPlanNameConflictException conflict =>
            new(WeeklyPlanFailureKind.Conflict, conflict.Code),
        _ => null,
    };
}
