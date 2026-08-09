namespace Friggy.Application.WeeklyPlans.Exceptions;

public sealed class WeeklyPlanNotFoundException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
