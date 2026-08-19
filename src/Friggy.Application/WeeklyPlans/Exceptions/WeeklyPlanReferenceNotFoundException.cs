namespace Friggy.Application.WeeklyPlans.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró una referencia necesaria para
/// modificar un plan semanal.
/// </summary>
public sealed class WeeklyPlanReferenceNotFoundException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
