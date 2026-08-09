using Friggy.Domain.Catalogs;
using Friggy.Domain.WeeklyPlans;

namespace Friggy.Domain.Tests.WeeklyPlans;

public sealed class WeeklyPlanTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_NameIsBlank_ThrowsDomainValidationException(string? name)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            WeeklyPlan.Create(name, new DateOnly(2026, 8, 3), null));

        Assert.Equal("weekly-plan.name.required", exception.Code);
    }

    [Fact]
    public void Create_NameIsTooLong_ThrowsDomainValidationException()
    {
        var name = new string('a', CatalogName.MaximumLength + 1);

        var exception = Assert.Throws<DomainValidationException>(() =>
            WeeklyPlan.Create(name, new DateOnly(2026, 8, 3), null));

        Assert.Equal("weekly-plan.name.too-long", exception.Code);
    }

    [Fact]
    public void Create_StartDateIsNotMonday_ThrowsDomainValidationException()
    {
        var tuesday = new DateOnly(2026, 8, 4);

        var exception = Assert.Throws<DomainValidationException>(() =>
            WeeklyPlan.Create("Semana 32", tuesday, null));

        Assert.Equal("weekly-plan.start-date.monday", exception.Code);
    }

    [Fact]
    public void Create_ValidValues_CreatesSevenDayWeekAndNormalizesText()
    {
        var monday = new DateOnly(2026, 8, 3);

        var plan = WeeklyPlan.Create("  Semana 32  ", monday, "  Vacaciones  ");

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal("Semana 32", plan.Name.Value);
        Assert.Equal("SEMANA 32", plan.Name.Normalized);
        Assert.Equal("Vacaciones", plan.Description);
        Assert.Equal(monday, plan.StartDate);
        Assert.Equal(new DateOnly(2026, 8, 9), plan.EndDate);
        Assert.Equal(
            Enumerable.Range(0, 7).Select(monday.AddDays),
            plan.Dates);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_DescriptionIsMissing_NormalizesDescriptionToNull(string? description)
    {
        var plan = WeeklyPlan.Create("Semana 32", new DateOnly(2026, 8, 3), description);

        Assert.Null(plan.Description);
    }
}
