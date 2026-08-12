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
    public void Assign_NewSlot_DefaultsToOneServing()
    {
        var plan = CreatePlan();

        plan.Assign(plan.StartDate, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(1, Assert.Single(plan.Entries).Servings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Assign_NonPositiveServings_ThrowsAndPreservesSchedule(int servings)
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(plan.StartDate, Guid.NewGuid(), Guid.NewGuid(), servings));

        Assert.Equal("weekly-plan.entry.servings.positive", exception.Code);
        Assert.Empty(plan.Entries);
    }

    [Fact]
    public void Assign_ExistingSlot_UpdatesRecipeAndServings()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid(), 2);
        var entryId = Assert.Single(plan.Entries).Id;
        var replacementRecipeId = Guid.NewGuid();

        plan.Assign(plan.StartDate, mealTypeId, replacementRecipeId, 4);

        var entry = Assert.Single(plan.Entries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(replacementRecipeId, entry.RecipeId);
        Assert.Equal(4, entry.Servings);
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

    [Fact]
    public void AddSlot_ValidMealType_AppendsStableSlotToSelectedDay()
    {
        var plan = CreatePlan();
        var date = plan.StartDate.AddDays(1);
        var breakfastId = Guid.NewGuid();
        var lunchId = Guid.NewGuid();

        var breakfast = plan.AddSlot(date, breakfastId);
        var lunch = plan.AddSlot(date, lunchId);

        Assert.NotEqual(Guid.Empty, breakfast.Id);
        Assert.Equal(plan.Id, breakfast.WeeklyPlanId);
        Assert.Equal(date, breakfast.Date);
        Assert.Equal(breakfastId, breakfast.MealTypeId);
        Assert.Equal(0, breakfast.Order);
        Assert.Equal(1, lunch.Order);
        Assert.Equal([breakfast.Id, lunch.Id], plan.Slots.Select(slot => slot.Id));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void AddSlot_DateIsOutsideWeek_ThrowsAndDoesNotMutate(int dayOffset)
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.AddSlot(plan.StartDate.AddDays(dayOffset), Guid.NewGuid()));

        Assert.Equal("weekly-plan.slot.date.out-of-range", exception.Code);
        Assert.Empty(plan.Slots);
    }

    [Fact]
    public void AddSlot_MealTypeIdIsEmpty_ThrowsAndDoesNotMutate()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.AddSlot(plan.StartDate, Guid.Empty));

        Assert.Equal("weekly-plan.slot.meal-type-id.required", exception.Code);
        Assert.Empty(plan.Slots);
    }

    [Fact]
    public void AddSlot_DuplicateMealTypeOnSameDay_ThrowsAndPreservesExistingSlot()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var existing = plan.AddSlot(plan.StartDate, mealTypeId);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.AddSlot(plan.StartDate, mealTypeId));

        Assert.Equal("weekly-plan.slot.meal-type.duplicate", exception.Code);
        Assert.Equal(existing.Id, Assert.Single(plan.Slots).Id);
    }

    [Fact]
    public void AddSlot_SameMealTypeOnDifferentDays_IsAllowedAndEachDayStartsAtZero()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();

        var monday = plan.AddSlot(plan.StartDate, mealTypeId);
        var tuesday = plan.AddSlot(plan.StartDate.AddDays(1), mealTypeId);

        Assert.Equal(0, monday.Order);
        Assert.Equal(0, tuesday.Order);
        Assert.NotEqual(monday.Id, tuesday.Id);
    }

    [Fact]
    public void ReorderSlots_CompleteDayPermutation_UpdatesOrderAndPreservesStableIdsAndAssignment()
    {
        var plan = CreatePlan();
        var breakfastId = Guid.NewGuid();
        var lunchId = Guid.NewGuid();
        var dinnerId = Guid.NewGuid();
        var breakfast = plan.AddSlot(plan.StartDate, breakfastId);
        var lunch = plan.AddSlot(plan.StartDate, lunchId);
        var dinner = plan.AddSlot(plan.StartDate, dinnerId);
        var recipeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, lunchId, recipeId, 2);

        plan.ReorderSlots(plan.StartDate, [dinner.Id, breakfast.Id, lunch.Id]);

        Assert.Equal(
            [dinner.Id, breakfast.Id, lunch.Id],
            plan.Slots
                .Where(slot => slot.Date == plan.StartDate)
                .OrderBy(slot => slot.Order)
                .Select(slot => slot.Id));
        Assert.Equal([0, 1, 2], plan.Slots.OrderBy(slot => slot.Order).Select(slot => slot.Order));
        var assignment = Assert.Single(plan.Entries);
        Assert.Equal(lunchId, assignment.MealTypeId);
        Assert.Equal(recipeId, assignment.RecipeId);
        Assert.Equal(2, assignment.Servings);
    }

    [Fact]
    public void ReorderSlots_DifferentDay_DoesNotChangeOtherDaysOrder()
    {
        var plan = CreatePlan();
        var mondayFirst = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var mondaySecond = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var tuesdayFirst = plan.AddSlot(plan.StartDate.AddDays(1), Guid.NewGuid());
        var tuesdaySecond = plan.AddSlot(plan.StartDate.AddDays(1), Guid.NewGuid());

        plan.ReorderSlots(plan.StartDate, [mondaySecond.Id, mondayFirst.Id]);

        Assert.Equal(0, tuesdayFirst.Order);
        Assert.Equal(1, tuesdaySecond.Order);
    }

    [Fact]
    public void ReorderSlots_IncompletePermutation_ThrowsAndDoesNotMutate()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var second = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.ReorderSlots(plan.StartDate, [second.Id]));

        Assert.Equal("weekly-plan.slot.order.invalid", exception.Code);
        Assert.Equal([(first.Id, 0), (second.Id, 1)], plan.Slots.Select(slot => (slot.Id, slot.Order)));
    }

    [Fact]
    public void ReorderSlots_DateIsOutsideWeek_ThrowsAndDoesNotMutate()
    {
        var plan = CreatePlan();
        var existing = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.ReorderSlots(plan.StartDate.AddDays(7), [existing.Id]));

        Assert.Equal("weekly-plan.slot.date.out-of-range", exception.Code);
        var preserved = Assert.Single(plan.Slots);
        Assert.Equal((existing.Id, 0), (preserved.Id, preserved.Order));
    }

    [Fact]
    public void ReorderSlots_RepeatedOrForeignIds_ThrowAndDoNotMutate()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var second = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var tuesday = plan.AddSlot(plan.StartDate.AddDays(1), Guid.NewGuid());

        var repeatedException = Assert.Throws<DomainValidationException>(() =>
            plan.ReorderSlots(plan.StartDate, [first.Id, first.Id]));
        var foreignException = Assert.Throws<DomainValidationException>(() =>
            plan.ReorderSlots(plan.StartDate, [second.Id, tuesday.Id]));

        Assert.Equal("weekly-plan.slot.order.invalid", repeatedException.Code);
        Assert.Equal("weekly-plan.slot.order.invalid", foreignException.Code);
        Assert.Equal([(first.Id, 0), (second.Id, 1)], plan.Slots
            .Where(slot => slot.Date == plan.StartDate)
            .Select(slot => (slot.Id, slot.Order)));
    }

    [Fact]
    public void RemoveSlot_EmptySlot_RemovesItAndCompactsDayOrder()
    {
        var plan = CreatePlan();
        var first = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var removed = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var last = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var wasRemoved = plan.RemoveSlot(removed.Id);

        Assert.True(wasRemoved);
        Assert.Equal([(first.Id, 0), (last.Id, 1)], plan.Slots.Select(slot => (slot.Id, slot.Order)));
    }

    [Fact]
    public void RemoveSlot_MissingSlot_ReturnsFalseAndPreservesSlots()
    {
        var plan = CreatePlan();
        var existing = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var wasRemoved = plan.RemoveSlot(Guid.NewGuid());

        Assert.False(wasRemoved);
        Assert.Equal(existing.Id, Assert.Single(plan.Slots).Id);
    }

    [Fact]
    public void RemoveSlot_IdIsEmpty_ThrowsAndPreservesSlots()
    {
        var plan = CreatePlan();
        var existing = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveSlot(Guid.Empty));

        Assert.Equal("weekly-plan.slot.id.required", exception.Code);
        Assert.Equal(existing.Id, Assert.Single(plan.Slots).Id);
    }

    [Fact]
    public void RemoveSlot_AssignedSlot_ThrowsUntilAssignmentIsExplicitlyRemoved()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var slot = plan.AddSlot(plan.StartDate, mealTypeId);
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveSlot(slot.Id));

        Assert.Equal("weekly-plan.slot.assigned", exception.Code);
        Assert.Single(plan.Slots);
        Assert.Single(plan.Entries);

        Assert.True(plan.RemoveEntry(plan.StartDate, mealTypeId));
        Assert.True(plan.RemoveSlot(slot.Id));
        Assert.Empty(plan.Slots);
    }

    [Fact]
    public void RemoveSlot_CompletedSlot_ThrowsAndPreservesSlotAndEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var slot = plan.AddSlot(plan.StartDate, mealTypeId);
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        plan.CompleteEntry(plan.StartDate, mealTypeId, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveSlot(slot.Id));

        Assert.Equal("weekly-plan.slot.completed", exception.Code);
        Assert.Equal(slot.Id, Assert.Single(plan.Slots).Id);
        Assert.True(Assert.Single(plan.Entries).IsCompleted);
    }

    [Fact]
    public void Assign_MissingSlot_CreatesCompatibilitySlotWithoutDuplicatingItOnReplace()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();

        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        var slotId = Assert.Single(plan.Slots).Id;
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid(), 3);

        var slot = Assert.Single(plan.Slots);
        Assert.Equal(slotId, slot.Id);
        Assert.Equal(0, slot.Order);
        Assert.Equal(3, Assert.Single(plan.Entries).Servings);
    }

    [Fact]
    public void Slots_CannotBeMutatedFromOutsideAggregate()
    {
        var plan = CreatePlan();
        plan.AddSlot(plan.StartDate, Guid.NewGuid());

        Assert.Throws<NotSupportedException>(() =>
            ((IList<MealPlanSlot>)plan.Slots).Clear());
        Assert.Single(plan.Slots);
    }

    [Fact]
    public void SetSlotTime_ValidLocalTime_StoresTimeAndPreservesItWhenReordered()
    {
        var plan = CreatePlan();
        var breakfast = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var lunch = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        var plannedTime = new TimeOnly(14, 5);

        var scheduled = plan.SetSlotTime(lunch.Id, plannedTime);
        plan.ReorderSlots(plan.StartDate, [lunch.Id, breakfast.Id]);

        Assert.Same(lunch, scheduled);
        Assert.Equal(plannedTime, lunch.PlannedTime);
        Assert.Equal(lunch.Id, plan.Slots.OrderBy(slot => slot.Order).First().Id);
    }

    [Fact]
    public void SetSlotTime_Null_ClearsExistingTime()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        plan.SetSlotTime(slot.Id, new TimeOnly(14, 5));

        plan.SetSlotTime(slot.Id, null);

        Assert.Null(slot.PlannedTime);
    }

    [Fact]
    public void SetSlotTime_MissingSlot_ThrowsAndPreservesSchedule()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SetSlotTime(Guid.NewGuid(), new TimeOnly(14, 5)));

        Assert.Equal("weekly-plan.slot.not-found", exception.Code);
        Assert.Null(slot.PlannedTime);
    }

    [Fact]
    public void SetSlotTime_EmptyId_ThrowsAndPreservesSchedule()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SetSlotTime(Guid.Empty, new TimeOnly(14, 5)));

        Assert.Equal("weekly-plan.slot.id.required", exception.Code);
        Assert.Null(slot.PlannedTime);
    }

    [Fact]
    public void GetPreparationStartsAt_PlannedRecipe_ReturnsUnspecifiedLocalDateTime()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        plan.SetSlotTime(slot.Id, new TimeOnly(14, 5));

        var preparationStartsAt = slot.GetPreparationStartsAt(TimeSpan.FromMinutes(45));

        Assert.Equal(new DateTime(2026, 8, 3, 13, 20, 0, DateTimeKind.Unspecified), preparationStartsAt);
        Assert.Equal(DateTimeKind.Unspecified, preparationStartsAt?.Kind);
    }

    [Fact]
    public void GetPreparationStartsAt_CrossesMidnight_ReturnsPreviousLocalDay()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        plan.SetSlotTime(slot.Id, new TimeOnly(0, 30));

        var preparationStartsAt = slot.GetPreparationStartsAt(TimeSpan.FromHours(1));

        Assert.Equal(new DateTime(2026, 8, 2, 23, 30, 0, DateTimeKind.Unspecified), preparationStartsAt);
    }

    [Fact]
    public void GetPreparationStartsAt_MissingTimeOrRecipe_ReturnsNull()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());

        Assert.Null(slot.GetPreparationStartsAt(TimeSpan.FromMinutes(30)));

        plan.SetSlotTime(slot.Id, new TimeOnly(14, 5));

        Assert.Null(slot.GetPreparationStartsAt(null));
    }

    [Fact]
    public void GetPreparationStartsAt_NegativeEstimatedTime_Throws()
    {
        var plan = CreatePlan();
        var slot = plan.AddSlot(plan.StartDate, Guid.NewGuid());
        plan.SetSlotTime(slot.Id, new TimeOnly(14, 5));

        var exception = Assert.Throws<DomainValidationException>(() =>
            slot.GetPreparationStartsAt(TimeSpan.FromMinutes(-1)));

        Assert.Equal("weekly-plan.slot.estimated-time.non-negative", exception.Code);
    }

    [Fact]
    public void CompleteEntry_AssignedMeal_StoresCompletionMoment()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid(), 2);
        var completedAt = new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);

        var entry = plan.CompleteEntry(plan.StartDate, mealTypeId, completedAt);

        Assert.True(entry.IsCompleted);
        Assert.Equal(completedAt, entry.CompletedAt);
    }

    [Fact]
    public void CompleteEntry_AlreadyCompleted_ThrowsAndPreservesOriginalMoment()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        var completedAt = new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);
        plan.CompleteEntry(plan.StartDate, mealTypeId, completedAt);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.CompleteEntry(plan.StartDate, mealTypeId, completedAt.AddHours(1)));

        Assert.Equal("weekly-plan.entry.already-completed", exception.Code);
        Assert.Equal(completedAt, Assert.Single(plan.Entries).CompletedAt);
    }

    [Fact]
    public void Assign_CompletedSlot_ThrowsAndPreservesEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, recipeId);
        plan.CompleteEntry(plan.StartDate, mealTypeId, DateTimeOffset.UtcNow);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid(), 2));

        Assert.Equal("weekly-plan.entry.completed", exception.Code);
        Assert.Equal(recipeId, Assert.Single(plan.Entries).RecipeId);
    }

    [Fact]
    public void SkipEntry_PlannedMeal_StoresNormalizedReasonAndAlternative()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());

        var entry = plan.SkipEntry(
            plan.StartDate,
            mealTypeId,
            "  Comida fuera de casa  ",
            "  Bocadillo  ");

        Assert.Equal(MealPlanEntryStatus.Skipped, entry.Status);
        Assert.True(entry.IsSkipped);
        Assert.False(entry.IsCompleted);
        Assert.Null(entry.CompletedAt);
        Assert.Equal("Comida fuera de casa", entry.SkippedReason);
        Assert.Equal("Bocadillo", entry.AlternativeDescription);
    }

    [Fact]
    public void SkipEntry_BlankReason_ThrowsWithoutMutatingEntry()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SkipEntry(plan.StartDate, mealTypeId, " ", "Bocadillo"));

        Assert.Equal("weekly-plan.entry.skipped-reason.required", exception.Code);
        var entry = Assert.Single(plan.Entries);
        Assert.Equal(MealPlanEntryStatus.Planned, entry.Status);
        Assert.Null(entry.SkippedReason);
        Assert.Null(entry.AlternativeDescription);
    }

    [Fact]
    public void SkipEntry_BlankAlternative_StoresNull()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());

        var entry = plan.SkipEntry(plan.StartDate, mealTypeId, "Sin hambre", " ");

        Assert.Null(entry.AlternativeDescription);
    }

    [Fact]
    public void SkipEntry_MissingAssignment_Throws()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SkipEntry(plan.StartDate, Guid.NewGuid(), "Sin hambre", null));

        Assert.Equal("weekly-plan.entry.not-assigned", exception.Code);
    }

    [Fact]
    public void SkipEntry_CompletedMeal_ThrowsAndPreservesCompletion()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var completedAt = new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero);
        plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid());
        plan.CompleteEntry(plan.StartDate, mealTypeId, completedAt);

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.SkipEntry(plan.StartDate, mealTypeId, "Cambio de planes", null));

        Assert.Equal("weekly-plan.entry.completed", exception.Code);
        var entry = Assert.Single(plan.Entries);
        Assert.Equal(completedAt, entry.CompletedAt);
        Assert.False(entry.IsSkipped);
    }

    [Fact]
    public void SkippedEntry_IsIrreversibleAndPreservesAssignment()
    {
        var plan = CreatePlan();
        var mealTypeId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        plan.Assign(plan.StartDate, mealTypeId, recipeId, 2);
        var slotId = Assert.Single(plan.Slots).Id;
        plan.SkipEntry(plan.StartDate, mealTypeId, "Cambio de planes", null);

        var completeException = Assert.Throws<DomainValidationException>(() =>
            plan.CompleteEntry(plan.StartDate, mealTypeId, DateTimeOffset.UtcNow));
        var assignException = Assert.Throws<DomainValidationException>(() =>
            plan.Assign(plan.StartDate, mealTypeId, Guid.NewGuid()));
        var removeException = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveEntry(plan.StartDate, mealTypeId));
        var slotException = Assert.Throws<DomainValidationException>(() =>
            plan.RemoveSlot(slotId));

        Assert.Equal("weekly-plan.entry.skipped", completeException.Code);
        Assert.Equal("weekly-plan.entry.skipped", assignException.Code);
        Assert.Equal("weekly-plan.entry.skipped", removeException.Code);
        Assert.Equal("weekly-plan.slot.skipped", slotException.Code);
        var entry = Assert.Single(plan.Entries);
        Assert.Equal(recipeId, entry.RecipeId);
        Assert.Equal(2, entry.Servings);
        Assert.True(entry.IsSkipped);
    }

    [Fact]
    public void UpdateDetails_ValidValues_ChangesTextAndPreservesSchedule()
    {
        var plan = CreatePlan();
        plan.Assign(plan.StartDate, Guid.NewGuid(), Guid.NewGuid());
        var planId = plan.Id;
        var entryId = Assert.Single(plan.Entries).Id;

        plan.UpdateDetails("  Semana de vacaciones  ", "  Costa  ");

        Assert.Equal(planId, plan.Id);
        Assert.Equal("Semana de vacaciones", plan.Name.Value);
        Assert.Equal("Costa", plan.Description);
        Assert.Equal(new DateOnly(2026, 8, 3), plan.StartDate);
        Assert.Equal(entryId, Assert.Single(plan.Entries).Id);
    }

    [Fact]
    public void UpdateDetails_InvalidName_ThrowsAndPreservesState()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<DomainValidationException>(() =>
            plan.UpdateDetails(" ", "Nueva descripción"));

        Assert.Equal("weekly-plan.name.required", exception.Code);
        Assert.Equal("Semana 32", plan.Name.Value);
        Assert.Null(plan.Description);
    }

    private static WeeklyPlan CreatePlan() =>
        WeeklyPlan.Create("Semana 32", new DateOnly(2026, 8, 3), null);
}
