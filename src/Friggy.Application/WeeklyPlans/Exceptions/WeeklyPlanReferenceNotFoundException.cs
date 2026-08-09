namespace Friggy.Application.WeeklyPlans.Exceptions;

public sealed class WeeklyPlanReferenceNotFoundException(string code, string message)
    : WeeklyPlanApplicationException(code, message);
