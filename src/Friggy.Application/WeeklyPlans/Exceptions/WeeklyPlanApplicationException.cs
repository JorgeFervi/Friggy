namespace Friggy.Application.WeeklyPlans.Exceptions;

public abstract class WeeklyPlanApplicationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
