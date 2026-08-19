namespace Friggy.Application.WeeklyPlans.Exceptions;

/// <summary>
/// Excepción que indica que no se encontró un plan semanal.
/// </summary>
public sealed class WeeklyPlanNotFoundException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
