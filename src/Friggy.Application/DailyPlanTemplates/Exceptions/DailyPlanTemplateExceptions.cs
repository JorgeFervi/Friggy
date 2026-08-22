namespace Friggy.Application.DailyPlanTemplates.Exceptions;

public abstract class DailyPlanTemplateApplicationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class DailyPlanTemplateNotFoundException(string code, string message)
    : DailyPlanTemplateApplicationException(code, message);

public sealed class DailyPlanTemplateConflictException(
    string code,
    string message,
    IReadOnlyList<DateOnly> conflictingDates)
    : DailyPlanTemplateApplicationException(code, message)
{
    public IReadOnlyList<DateOnly> ConflictingDates { get; } = conflictingDates;
}
