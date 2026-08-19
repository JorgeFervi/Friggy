namespace Friggy.Application.WeeklyPlans.Exceptions;

/// <summary>
/// Excepción que indica un conflicto con el nombre de un plan semanal.
/// </summary>
public sealed class WeeklyPlanNameConflictException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
