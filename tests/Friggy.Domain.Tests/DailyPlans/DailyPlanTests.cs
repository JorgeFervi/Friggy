using Friggy.Domain.Catalogs;
using Friggy.Domain.DailyPlans;

namespace Friggy.Domain.Tests.DailyPlans;

public sealed class DailyPlanTests
{
    [Fact]
    public void Create_AnyDate_CreatesSingleDayPlan()
    {
        var date = new DateOnly(2026, 8, 4);

        var plan = DailyPlan.Create(date);

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(date, plan.Date);
        Assert.Empty(plan.Entries);
        Assert.Empty(plan.Slots);
    }

    [Fact]
    public void Assign_ValidMeal_AddsEntryAndSlotOwnedByDailyPlan()
    {
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();

        plan.Assign(mealTypeId, recipeId, servings: 3);

        var entry = Assert.Single(plan.Entries);
        var slot = Assert.Single(plan.Slots);
        Assert.Equal(plan.Id, entry.DailyPlanId);
        Assert.Equal(plan.Id, slot.DailyPlanId);
        Assert.Equal(mealTypeId, entry.MealTypeId);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal(3, entry.Servings);
    }

    [Fact]
    public void Assign_SameMealType_ReplacesRecipeWithoutDuplicating()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(mealTypeId, Guid.NewGuid(), 2);
        var entryId = Assert.Single(plan.Entries).Id;
        var replacementId = Guid.NewGuid();

        plan.Assign(mealTypeId, replacementId, 4);

        var entry = Assert.Single(plan.Entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(replacementId, entry.RecipeId);
        Assert.Equal(4, entry.Servings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Assign_NonPositiveServings_ThrowsWithoutMutation(int servings)
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(Guid.NewGuid(), Guid.NewGuid(), servings));

        Assert.Equal("daily-plan.entry.servings.positive", exception.Code);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void AddSlot_DuplicateMealType_Throws()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.AddSlot(mealTypeId);

        var exception = Assert.Throws<DomainValidationException>(() => plan.AddSlot(mealTypeId));

        Assert.Equal("daily-plan.slot.meal-type.duplicate", exception.Code);
    }

    [Fact]
    public void ReorderSlots_AllSlotsProvided_ChangesOrder()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(Guid.NewGuid());
        var second = plan.AddSlot(Guid.NewGuid());

        plan.ReorderSlots([second.Id, first.Id]);

        Assert.Equal(1, first.Order);
        Assert.Equal(0, second.Order);
    }

    [Fact]
    public void ReorderSlots_MissingSlot_ThrowsWithoutChangingOrder()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(Guid.NewGuid());
        var second = plan.AddSlot(Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.ReorderSlots([first.Id]));

        Assert.Equal("daily-plan.slot.order.invalid", exception.Code);
        Assert.Equal(0, first.Order);
        Assert.Equal(1, second.Order);
    }

    [Fact]
    public void RemoveSlot_UnassignedSlot_RemovesAndCompactsOrder()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(Guid.NewGuid());
        var second = plan.AddSlot(Guid.NewGuid());

        Assert.True(plan.RemoveSlot(first.Id));

        Assert.Equal(0, Assert.Single(plan.Slots).Order);
        Assert.Equal(second.Id, Assert.Single(plan.Slots).Id);
    }

    [Fact]
    public void RemoveSlot_AssignedSlot_Throws()
    {
        var plan = CreatePlan();
        plan.Assign(Guid.NewGuid(), Guid.NewGuid());
        var slot = Assert.Single(plan.Slots);

        var exception = Assert.Throws<DomainValidationException>(() => plan.RemoveSlot(slot.Id));

        Assert.Equal("daily-plan.slot.assigned", exception.Code);
    }

    [Fact]
    public void SetSlotTime_WithRecipeEstimate_CalculatesPreparationAcrossMidnight()
    {
        var plan = DailyPlan.Create(new DateOnly(2026, 8, 4));
        var slot = plan.AddSlot(Guid.NewGuid());
        plan.SetSlotTime(slot.Id, new TimeOnly(0, 15));

        var startsAt = slot.GetPreparationStartsAt(plan.Date, TimeSpan.FromMinutes(30));

        Assert.Equal(new DateTime(2026, 8, 3, 23, 45, 0), startsAt);
    }

    [Fact]
    public void RemoveEntry_PlannedEntry_RemovesAssignmentAndKeepsSlot()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(mealTypeId, Guid.NewGuid());

        Assert.True(plan.RemoveEntry(mealTypeId));

        Assert.Empty(plan.Entries);
        Assert.Single(plan.Slots);
    }

    [Fact]
    public void SkipEntry_ValidReason_NormalizesMetadataAndLocksEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(mealTypeId, Guid.NewGuid());

        var entry = plan.SkipEntry(mealTypeId, "  Viaje  ", "  Bocadillo  ");

        Assert.True(entry.IsSkipped);
        Assert.Equal("Viaje", entry.SkippedReason);
        Assert.Equal("Bocadillo", entry.AlternativeDescription);
        Assert.Throws<DomainValidationException>(() =>
            plan.Assign(mealTypeId, Guid.NewGuid()));
    }

    [Fact]
    public void SkipEntry_BlankReason_ThrowsAndPreservesPlannedState()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(mealTypeId, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SkipEntry(mealTypeId, " ", null));

        Assert.Equal("daily-plan.entry.skipped-reason.required", exception.Code);
        Assert.False(Assert.Single(plan.Entries).IsSkipped);
    }

    [Fact]
    public void CompleteEntry_PlannedEntry_RecordsInstantAndLocksChanges()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var completedAt = new DateTimeOffset(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);
        plan.Assign(mealTypeId, Guid.NewGuid());

        var entry = plan.CompleteEntry(mealTypeId, completedAt);

        Assert.True(entry.IsCompleted);
        Assert.Equal(completedAt, entry.CompletedAt);
        Assert.Throws<DomainValidationException>(() => plan.RemoveEntry(mealTypeId));
    }

    [Fact]
    public void CompleteEntry_AlreadyCompleted_Throws()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(mealTypeId, Guid.NewGuid());
        plan.CompleteEntry(mealTypeId, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.CompleteEntry(mealTypeId, DateTimeOffset.UtcNow));

        Assert.Equal("daily-plan.entry.completed", exception.Code);
    }

    private static DailyPlan CreatePlan() =>
        DailyPlan.Create(new DateOnly(2026, 8, 4));
}
