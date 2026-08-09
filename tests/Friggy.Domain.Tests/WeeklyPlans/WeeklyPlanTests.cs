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

    [Fact]
    public void Assign_ValidCell_AddsEntryOwnedByPlan()
    {
        var plan = CreatePlan();
        var date = plan.StartDate.AddDays(1);
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        plan.Assign(date, mealTypeId, recipeId);

        var entry = Assert.Single(plan.Entries);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(plan.Id, entry.WeeklyPlanId);
        Assert.Equal(date, entry.Date);
        Assert.Equal(mealTypeId, entry.MealTypeId);
        Assert.Equal(recipeId, entry.RecipeId);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Assign_DateIsOutsideWeek_ThrowsAndDoesNotMutate(int dayOffset)
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(
                plan.StartDate.AddDays(dayOffset),
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Equal("weekly-plan.entry.date.out-of-range", exception.Code);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void Assign_MealTypeIdIsEmpty_ThrowsAndDoesNotMutate()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(plan.StartDate, Guid.Empty, Guid.NewGuid()));

        Assert.Equal("weekly-plan.entry.meal-type-id.required", exception.Code);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void Assign_RecipeIdIsEmpty_ThrowsAndDoesNotMutate()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(plan.StartDate, Guid.NewGuid(), Guid.Empty));

        Assert.Equal("weekly-plan.entry.recipe-id.required", exception.Code);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void Assign_SameRecipeToExistingSlot_DoesNotDuplicateEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, recipeId);
        var entryId = Assert.Single(plan.Entries).Id;

        plan.Assign(plan.StartDate, mealTypeId, recipeId);

        var entry = Assert.Single(plan.Entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(recipeId, entry.RecipeId);
    }

    [Fact]
    public void Assign_ExistingSlot_ReplacesRecipeWithoutDuplicatingEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var firstRecipeId = Guid.NewGuid();
        var replacementRecipeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, firstRecipeId);
        var entryId = Assert.Single(plan.Entries).Id;

        plan.Assign(plan.StartDate, mealTypeId, replacementRecipeId);

        var entry = Assert.Single(plan.Entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(replacementRecipeId, entry.RecipeId);
    }

    [Fact]
    public void Remove_ExistingSlot_RemovesOnlySelectedEntry()
    {
        var plan = CreatePlan();
        var breakfastId = Guid.NewGuid();
        var lunchId = Guid.NewGuid();
        plan.Assign(plan.StartDate, breakfastId, Guid.NewGuid());
        plan.Assign(plan.StartDate, lunchId, Guid.NewGuid());

        var removed = plan.RemoveEntry(plan.StartDate, breakfastId);

        Assert.True(removed);
        Assert.Equal(lunchId, Assert.Single(plan.Entries).MealTypeId);
    }

    [Fact]
    public void Remove_MissingSlot_ReturnsFalseAndPreservesEntries()
    {
        var plan = CreatePlan();
        var assignedMealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, assignedMealTypeId, Guid.NewGuid());
        var entryId = Assert.Single(plan.Entries).Id;

        var removed = plan.RemoveEntry(plan.StartDate, Guid.NewGuid());

        Assert.False(removed);
        Assert.Equal(entryId, Assert.Single(plan.Entries).Id);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Remove_DateIsOutsideWeek_ThrowsAndPreservesEntries(int dayOffset)
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        var entryId = Assert.Single(plan.Entries).Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveEntry(plan.StartDate.AddDays(dayOffset), mealTypeId));

        Assert.Equal("weekly-plan.entry.date.out-of-range", exception.Code);
        Assert.Equal(entryId, Assert.Single(plan.Entries).Id);
    }

    [Fact]
    public void Remove_MealTypeIdIsEmpty_ThrowsAndPreservesEntries()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        var entryId = Assert.Single(plan.Entries).Id;

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveEntry(plan.StartDate, Guid.Empty));

        Assert.Equal("weekly-plan.entry.meal-type-id.required", exception.Code);
        Assert.Equal(entryId, Assert.Single(plan.Entries).Id);
    }

    [Fact]
    public void Entries_CannotBeMutatedFromOutsideAggregate()
    {
        var plan = CreatePlan();
        plan.Assign(plan.StartDate, Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<NotSupportedException>(() =>
            ((IList<MealPlanEntry>)plan.Entries).Clear());
        Assert.Single(plan.Entries);
    }

    private static WeeklyPlan CreatePlan() =>
        WeeklyPlan.Create("Semana 32", new DateOnly(2026, 8, 3), null);
}
