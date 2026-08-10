using System.ComponentModel.DataAnnotations;
using Friggy.Application.WeeklyPlans.Dtos;

namespace Friggy.Web.WeeklyPlans;

public sealed class WeeklyPlanFormModel : IValidatableObject
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar 120 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; } = GetDefaultStartDate();

    public string? Description { get; set; }

    public CreateWeeklyPlanRequest ToCreateRequest() =>
        new(Name, StartDate, Description);

    public UpdateWeeklyPlanRequest ToUpdateRequest() =>
        new(Name, Description);

    public static WeeklyPlanFormModel FromResponse(WeeklyPlanResponse response) =>
        new()
        {
            Name = response.Name,
            StartDate = response.StartDate,
            Description = response.Description,
        };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.DayOfWeek is not DayOfWeek.Monday)
        {
            yield return new ValidationResult(
                "La semana debe comenzar en lunes.",
                [nameof(StartDate)]);
        }
    }

    private static DateOnly GetDefaultStartDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilMonday);
    }
}
