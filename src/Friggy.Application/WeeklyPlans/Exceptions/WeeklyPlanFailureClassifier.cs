namespace Friggy.Application.WeeklyPlans.Exceptions;

public enum WeeklyPlanFailureKind
{
    NotFound,
    Conflict,
}

public sealed record WeeklyPlanFailure(WeeklyPlanFailureKind Kind, string Code);

public static class WeeklyPlanFailureClassifier
{
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
