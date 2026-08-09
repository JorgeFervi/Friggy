namespace Friggy.Application.WeeklyPlans.Exceptions;

public sealed class WeeklyPlanNameConflictException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
