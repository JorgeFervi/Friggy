using Bunit;
using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Inventory.Dtos;
using Friggy.Application.Recipes.Dtos;
using Friggy.Application.WeeklyPlans.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.WeeklyPlans;

public sealed class WeeklyPlanPageTests : ComponentTest
{
    private static readonly Guid PlanId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid BreakfastId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid LunchId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid DinnerId = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid FirstRecipeId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondRecipeId = Guid.Parse("50000000-0000-0000-0000-000000000002");
    private static readonly DateOnly WeekStart = new(2026, 8, 3);

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlans_EmptyCollection_ShowsCreateFormAndEmptyState()
    {
        RegisterApis(new StubWeeklyPlansApiClient());

        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlans>();

        component.WaitForAssertion(() =>
        {
            Assert.NotNull(component.Find("form[data-testid='weekly-plan-create-form']"));
            Assert.Equal("Planes semanales", component.Find("h1").TextContent);
            Assert.NotNull(component.Find(".friggy-weekly-plans"));
            Assert.Contains("No hay planes semanales", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlans_ValidForm_CreatesPlanAndNavigatesToCalendar()
    {
        var api = new StubWeeklyPlansApiClient();
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlans>();
        component.WaitForElement("form[data-testid='weekly-plan-create-form']");

        component.Find("#weekly-plan-name").Change("Semana 32");
        component.Find("#weekly-plan-start-date").Change("2026-08-03");
        component.Find("#weekly-plan-description").Change("Plan principal");
        component.Find("form[data-testid='weekly-plan-create-form']").Submit();

        component.WaitForAssertion(() =>
        {
            var request = Assert.Single(api.Created);
            Assert.Equal("Semana 32", request.Name);
            Assert.Equal(WeekStart, request.StartDate);
            Assert.Equal("Plan principal", request.Description);
            Assert.EndsWith(
                $"/weekly-plans/{PlanId}",
                GetRequiredService<NavigationManager>().Uri,
                StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlans_StartDateIsNotMonday_ShowsValidationWithoutCallingApi()
    {
        var api = new StubWeeklyPlansApiClient();
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlans>();
        component.WaitForElement("form[data-testid='weekly-plan-create-form']");

        component.Find("#weekly-plan-name").Change("Semana inválida");
        component.Find("#weekly-plan-start-date").Change("2026-08-04");
        component.Find("form[data-testid='weekly-plan-create-form']").Submit();

        component.WaitForAssertion(() =>
            Assert.Contains("lunes", component.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(api.Created);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlans_DeleteConfirmed_DeletesAndRefreshesList()
    {
        var api = new StubWeeklyPlansApiClient { Plan = EmptyPlan() };
        RegisterApis(api);
        SetupConfirmDialog();
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlans>();
        component.WaitForElement("button[data-action='delete-weekly-plan']");

        component.Find("button[data-action='delete-weekly-plan']").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(PlanId, Assert.Single(api.Deleted));
            Assert.Contains("No hay planes semanales", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_PendingApi_ShowsLoadingThenSevenDayEmptyCalendar()
    {
        var pending = new TaskCompletionSource<WeeklyPlanResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new StubWeeklyPlansApiClient { PendingGet = pending };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));

        Assert.Contains("Cargando planificación", component.Markup, StringComparison.Ordinal);

        pending.SetResult(EmptyPlan());
        component.WaitForAssertion(() =>
        {
            Assert.Equal(7, component.FindAll("section[data-testid='weekly-plan-day']").Count);
            Assert.Equal(21, component.FindAll("[data-testid='meal-slot']").Count);
            Assert.All(
                component.FindAll("[data-testid='meal-slot'] select"),
                select => Assert.Equal(string.Empty, select.GetAttribute("value")));
            Assert.Contains("Sin receta", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_SelectAndRemoveRecipe_UpdatesCalendarThroughApi()
    {
        var api = new StubWeeklyPlansApiClient { Plan = EmptyPlan() };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var selector = CellSelector(WeekStart, LunchId);
        component.WaitForElement(selector);

        component.Find(selector).Change(FirstRecipeId.ToString());
        component.WaitForAssertion(() =>
            Assert.Equal(FirstRecipeId, Assert.Single(api.SetEntries).RecipeId));

        component.Find(selector).Change(SecondRecipeId.ToString());
        component.WaitForAssertion(() =>
        {
            Assert.Equal(2, api.SetEntries.Count);
            Assert.Equal(SecondRecipeId, api.SetEntries[1].RecipeId);
            Assert.Equal(SecondRecipeId.ToString(), component.Find(selector).GetAttribute("value"));
        });

        component.Find($"button[data-date='2026-08-03'][data-meal-type-id='{LunchId}']").Click();
        component.WaitForAssertion(() =>
        {
            Assert.Single(api.RemovedEntries);
            Assert.Equal(string.Empty, component.Find(selector).GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_AssignmentCompletes_ShowsSavedStatus()
    {
        var api = new StubWeeklyPlansApiClient { Plan = EmptyPlan() };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var selector = CellSelector(WeekStart, LunchId);
        component.WaitForElement(selector);

        component.Find(selector).Change(FirstRecipeId.ToString());

        component.WaitForAssertion(() =>
            Assert.Equal(
                "Asignación guardada.",
                component.Find("[role='status']").TextContent));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_AssignmentFails_ShowsErrorAndPreservesCalendar()
    {
        var api = new StubWeeklyPlansApiClient
        {
            Plan = EmptyPlan(),
            OperationException = new ApiProblemException("No se encontró la receta."),
        };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var selector = CellSelector(WeekStart, LunchId);
        component.WaitForElement(selector);

        component.Find(selector).Change(FirstRecipeId.ToString());

        component.WaitForAssertion(() =>
        {
            Assert.Contains("No se encontró la receta", component.Find("[role='alert']").TextContent, StringComparison.Ordinal);
            Assert.Equal(string.Empty, component.Find(selector).GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_CompleteMeal_ShowsOnlyCompatibleLotsAndReportsRemainder()
    {
        var assignedPlan = ReplaceRecipe(EmptyPlan(), WeekStart, LunchId, FirstRecipeId);
        var plans = new StubWeeklyPlansApiClient { Plan = assignedPlan };
        var recipes = new StubRecipesApiClient();
        var inventory = new StubInventoryApiClient();
        var completion = new StubWeeklyPlanInventoryApiClient
        {
            Completion = new MealCompletionResponse(
                Guid.NewGuid(),
                false,
                [],
                [new(Guid.NewGuid(), "Tomate", Guid.NewGuid(), "Gramo", "g", 0.5m)]),
        };
        inventory.Items.Add(new InventoryLotResponse(
            Guid.NewGuid(),
            recipes.IngredientId,
            "Tomate",
            recipes.UnitTypeId,
            "Gramo",
            "g",
            1m,
            new DateOnly(2026, 8, 20),
            false,
            []));
        inventory.Items.Add(new InventoryLotResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cebolla",
            recipes.UnitTypeId,
            "Gramo",
            "g",
            1m,
            new DateOnly(2026, 8, 20),
            false,
            []));
        RegisterApis(plans, recipes, completion, inventory);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        component.WaitForElement("button");

        component.FindAll("button").Single(button => button.TextContent.Trim() == "Completar").Click();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Tomate", component.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Cebolla", component.Markup, StringComparison.Ordinal);
        });
        component.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Confirmar finalización")
            .Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Queda por cubrir", component.Find("[role='status']").TextContent);
            Assert.Contains("g de Tomate", component.Find("[role='status']").TextContent);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_EditDetails_UpdatesHeaderAndPreservesCalendar()
    {
        var api = new StubWeeklyPlansApiClient { Plan = EmptyPlan() };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        component.WaitForElement("form[data-testid='weekly-plan-edit-form']");

        component.Find("#weekly-plan-edit-name").Change("Semana actualizada");
        component.Find("#weekly-plan-edit-description").Change("Nueva descripción");
        component.Find("form[data-testid='weekly-plan-edit-form']").Submit();

        component.WaitForAssertion(() =>
        {
            var request = Assert.Single(api.Updated);
            Assert.Equal("Semana actualizada", request.Name);
            Assert.Contains("Semana actualizada", component.Find("h1").TextContent, StringComparison.Ordinal);
            Assert.Equal(7, component.FindAll("section[data-testid='weekly-plan-day']").Count);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_SlotControls_AddReorderAndRemoveWithoutChangingOtherDays()
    {
        var initial = EmptyPlan();
        var firstDay = initial.Days[0] with { Meals = initial.Days[0].Meals.Take(2).ToArray() };
        var api = new StubWeeklyPlansApiClient
        {
            Plan = initial with { Days = [firstDay, .. initial.Days.Skip(1)] },
        };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var firstSection = component.WaitForElements("section[data-testid='weekly-plan-day']")[0];

        firstSection.QuerySelector("select[data-testid='add-meal-slot']")!.Change(DinnerId.ToString());
        component.WaitForAssertion(() =>
            Assert.Equal(3, component.FindAll("section[data-testid='weekly-plan-day']")[0]
                .QuerySelectorAll("[data-testid='meal-slot']").Length));

        component.Find("button[aria-label='Bajar Desayuno']").Click();
        component.WaitForAssertion(() =>
            Assert.Equal(
                ["Comida", "Desayuno", "Cena"],
                component.FindAll("section[data-testid='weekly-plan-day']")[0]
                    .QuerySelectorAll("[data-testid='meal-slot'] strong")
                    .Select(element => element.TextContent)));

        component.Find("button[aria-label='Eliminar hueco Cena']").Click();
        component.WaitForAssertion(() =>
            Assert.Equal(2, component.FindAll("section[data-testid='weekly-plan-day']")[0]
                .QuerySelectorAll("[data-testid='meal-slot']").Length));
        Assert.Equal(3, component.FindAll("section[data-testid='weekly-plan-day']")[1]
            .QuerySelectorAll("[data-testid='meal-slot']").Length);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_SkipFailure_ShowsRecoverableErrorAndPreservesDraft()
    {
        var assigned = ReplaceRecipe(EmptyPlan(), WeekStart, LunchId, FirstRecipeId);
        var api = new StubWeeklyPlansApiClient
        {
            Plan = assigned,
            OperationException = new ApiProblemException("El motivo es obligatorio."),
        };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var prefix = $"meal-{WeekStart:yyyyMMdd}-{LunchId:N}";
        var reason = component.WaitForElement($"#{prefix}-skip-reason");
        var alternative = component.Find($"#{prefix}-alternative");

        reason.Input("Cambio de planes");
        alternative.Input("Bocadillo");
        component.FindAll("button").Single(button => button.TextContent.Trim() == "Omitir").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("El motivo es obligatorio", component.Find("[role='alert']").TextContent);
            Assert.Equal("Cambio de planes", component.Find($"#{prefix}-skip-reason").GetAttribute("value"));
            Assert.Equal("Bocadillo", component.Find($"#{prefix}-alternative").GetAttribute("value"));
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void WeeklyPlanDetails_Schedule_ShowsPreparationStartAndSavesLocalTime()
    {
        var initial = EmptyPlan();
        var scheduled = initial with
        {
            Days = initial.Days.Select(day => day.Date != WeekStart
                ? day
                : day with
                {
                    Meals = day.Meals.Select(meal => meal.MealTypeId != LunchId
                        ? meal
                        : meal with
                        {
                            PlannedTime = "14:00",
                            PreparationStartsAt = new DateTime(2026, 8, 3, 13, 15, 0),
                        }).ToArray(),
                }).ToArray(),
        };
        var api = new StubWeeklyPlansApiClient { Plan = scheduled };
        RegisterApis(api);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlanDetails>(parameters =>
            parameters.Add(page => page.Id, PlanId));
        var prefix = $"meal-{WeekStart:yyyyMMdd}-{LunchId:N}";

        Assert.Contains("Empezar preparación: 03/08/2026 13:15", component.Markup);
        component.Find($"#{prefix}-time").Change("14:30");

        component.WaitForAssertion(() =>
            Assert.Equal("Hora prevista guardada.", component.Find("[role='status']").TextContent));
    }

    private void RegisterApis(
        StubWeeklyPlansApiClient weeklyPlans,
        StubRecipesApiClient? recipes = null,
        StubWeeklyPlanInventoryApiClient? weeklyPlanInventory = null,
        StubInventoryApiClient? inventory = null)
    {
        Services.AddSingleton<IWeeklyPlansApiClient>(weeklyPlans);
        Services.AddSingleton<IRecipesApiClient>(recipes ?? new StubRecipesApiClient());
        Services.AddSingleton<IMealTypesApiClient>(new StubMealTypesApiClient());
        Services.AddSingleton<IWeeklyPlanInventoryApiClient>(
            weeklyPlanInventory ?? new StubWeeklyPlanInventoryApiClient());
        Services.AddSingleton<IInventoryApiClient>(inventory ?? new StubInventoryApiClient());
    }

    private void SetupConfirmDialog()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
    }

    private static string CellSelector(DateOnly date, Guid mealTypeId) =>
        $"select[data-date='{date:yyyy-MM-dd}'][data-meal-type-id='{mealTypeId}']";

    private static WeeklyPlanResponse EmptyPlan() =>
        new(
            PlanId,
            "Semana 32",
            WeekStart,
            WeekStart.AddDays(6),
            "Plan inicial",
            Enumerable.Range(0, 7)
                .Select(offset => new WeeklyPlanDayResponse(
                    WeekStart.AddDays(offset),
                    [
                        CreateMeal(BreakfastId, "Desayuno", 0),
                        CreateMeal(LunchId, "Comida", 1),
                        CreateMeal(DinnerId, "Cena", 2),
                    ]))
                .ToArray());

    private static WeeklyPlanResponse ReplaceRecipe(
        WeeklyPlanResponse plan,
        DateOnly date,
        Guid mealTypeId,
        Guid? recipeId) =>
        plan with
        {
            Days = plan.Days
                .Select(day => day.Date != date
                    ? day
                    : day with
                    {
                        Meals = day.Meals
                            .Select(meal => meal.MealTypeId == mealTypeId
                                ? meal with { RecipeId = recipeId }
                                : meal)
                            .ToArray(),
                    })
                .ToArray(),
        };

    private static WeeklyPlanMealResponse CreateMeal(
        Guid mealTypeId,
        string mealTypeName,
        int order) =>
        new(
            mealTypeId,
            mealTypeName,
            order,
            null,
            1,
            false,
            null,
            Guid.NewGuid(),
            order,
            null,
            null,
            MealPlanEntryState.Planned,
            null,
            null);

    private sealed class StubWeeklyPlansApiClient : IWeeklyPlansApiClient
    {
        public WeeklyPlanResponse? Plan { get; set; }
        public TaskCompletionSource<WeeklyPlanResponse>? PendingGet { get; init; }
        public ApiProblemException? OperationException { get; init; }
        public List<CreateWeeklyPlanRequest> Created { get; } = [];
        public List<UpdateWeeklyPlanRequest> Updated { get; } = [];
        public List<Guid> Deleted { get; } = [];
        public List<(DateOnly Date, Guid MealTypeId, Guid RecipeId)> SetEntries { get; } = [];
        public List<(DateOnly Date, Guid MealTypeId)> RemovedEntries { get; } = [];

        public Task<IReadOnlyList<WeeklyPlanListItemResponse>> ListAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<WeeklyPlanListItemResponse> plans = Plan is null
                ? []
                : [new(Plan.Id, Plan.Name, Plan.StartDate, Plan.EndDate)];
            return Task.FromResult(plans);
        }

        public Task<WeeklyPlanResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
            PendingGet?.Task ?? Task.FromResult(
                Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal."));

        public Task<WeeklyPlanResponse> CreateAsync(
            CreateWeeklyPlanRequest request,
            CancellationToken cancellationToken)
        {
            Created.Add(request);
            Plan = EmptyPlan() with
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.StartDate.AddDays(6),
                Description = request.Description,
            };
            return Task.FromResult(Plan);
        }

        public Task<WeeklyPlanResponse> UpdateAsync(
            Guid id,
            UpdateWeeklyPlanRequest request,
            CancellationToken cancellationToken)
        {
            Updated.Add(request);
            Plan = (Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.")) with
            {
                Name = request.Name,
                Description = request.Description,
            };
            return Task.FromResult(Plan);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Deleted.Add(id);
            Plan = null;
            return Task.CompletedTask;
        }

        public Task<WeeklyPlanResponse> SetEntryAsync(
            Guid planId,
            DateOnly date,
            Guid mealTypeId,
            SetMealPlanEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (OperationException is not null)
            {
                throw OperationException;
            }

            SetEntries.Add((date, mealTypeId, request.RecipeId));
            Plan = ReplaceRecipe(
                Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal."),
                date,
                mealTypeId,
                request.RecipeId);
            return Task.FromResult(Plan);
        }

        public Task<WeeklyPlanResponse> RemoveEntryAsync(
            Guid planId,
            DateOnly date,
            Guid mealTypeId,
            CancellationToken cancellationToken)
        {
            RemovedEntries.Add((date, mealTypeId));
            Plan = ReplaceRecipe(
                Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal."),
                date,
                mealTypeId,
                null);
            return Task.FromResult(Plan);
        }

        public Task<WeeklyPlanResponse> AddSlotAsync(
            Guid planId,
            DateOnly mealDate,
            AddMealPlanSlotRequest request,
            CancellationToken cancellationToken)
        {
            ThrowIfConfigured();
            var current = Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.");
            Plan = current with
            {
                Days = current.Days.Select(day => day.Date != mealDate
                    ? day
                    : day with
                    {
                        Meals = [.. day.Meals, new(
                            request.MealTypeId,
                            MealTypeName(request.MealTypeId),
                            day.Meals.Count,
                            null,
                            Servings: 1,
                            IsCompleted: false,
                            CompletedAt: null,
                            SlotId: Guid.NewGuid(),
                            SlotOrder: day.Meals.Count,
                            PlannedTime: null,
                            PreparationStartsAt: null,
                            Status: MealPlanEntryState.Planned,
                            SkippedReason: null,
                            AlternativeDescription: null)],
                    }).ToArray(),
            };
            return Task.FromResult(Plan);
        }

        public Task<WeeklyPlanResponse> ReorderSlotsAsync(
            Guid planId,
            DateOnly mealDate,
            ReorderMealPlanSlotsRequest request,
            CancellationToken cancellationToken)
        {
            ThrowIfConfigured();
            var current = Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.");
            Plan = current with
            {
                Days = current.Days.Select(day => day.Date != mealDate
                    ? day
                    : day with
                    {
                        Meals = request.SlotIds.Select((id, order) =>
                            day.Meals.Single(meal => meal.SlotId == id) with { SlotOrder = order }).ToArray(),
                    }).ToArray(),
            };
            return Task.FromResult(Plan);
        }

        public Task<WeeklyPlanResponse> RemoveSlotAsync(
            Guid planId,
            Guid slotId,
            CancellationToken cancellationToken)
        {
            ThrowIfConfigured();
            var current = Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.");
            Plan = current with
            {
                Days = current.Days.Select(day => day with
                {
                    Meals = day.Meals.Where(meal => meal.SlotId != slotId).ToArray(),
                }).ToArray(),
            };
            return Task.FromResult(Plan);
        }

        public Task<MealPlanSlotScheduleResponse> SetSlotTimeAsync(
            Guid planId,
            Guid slotId,
            SetMealPlanSlotTimeRequest request,
            CancellationToken cancellationToken)
        {
            ThrowIfConfigured();
            var current = Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.");
            Plan = current with
            {
                Days = current.Days.Select(day => day with
                {
                    Meals = day.Meals.Select(meal => meal.SlotId == slotId
                        ? meal with { PlannedTime = request.PlannedTime }
                        : meal).ToArray(),
                }).ToArray(),
            };
            return Task.FromResult(new MealPlanSlotScheduleResponse(slotId, request.PlannedTime, null));
        }

        public Task<MealPlanEntryStateResponse> SkipEntryAsync(
            Guid planId,
            DateOnly mealDate,
            Guid mealTypeId,
            SkipMealPlanEntryRequest request,
            CancellationToken cancellationToken)
        {
            ThrowIfConfigured();
            var current = Plan ?? throw new InvalidOperationException("Falta configurar el plan semanal.");
            MealPlanEntryStateResponse? result = null;
            Plan = current with
            {
                Days = current.Days.Select(day => day.Date != mealDate
                    ? day
                    : day with
                    {
                        Meals = day.Meals.Select(meal => meal.MealTypeId != mealTypeId
                            ? meal
                            : SetSkipped(meal, request, out result)).ToArray(),
                    }).ToArray(),
            };
            return Task.FromResult(result ?? throw new InvalidOperationException("Falta la asignación."));
        }

        private void ThrowIfConfigured()
        {
            if (OperationException is not null)
            {
                throw OperationException;
            }
        }

        private static WeeklyPlanMealResponse SetSkipped(
            WeeklyPlanMealResponse meal,
            SkipMealPlanEntryRequest request,
            out MealPlanEntryStateResponse result)
        {
            result = new(
                Guid.NewGuid(),
                MealPlanEntryState.Skipped,
                null,
                request.Reason,
                request.AlternativeDescription);
            return meal with
            {
                Status = MealPlanEntryState.Skipped,
                SkippedReason = request.Reason,
                AlternativeDescription = request.AlternativeDescription,
            };
        }

        private static string MealTypeName(Guid id) => id == BreakfastId
            ? "Desayuno"
            : id == LunchId
                ? "Comida"
                : id == DinnerId
                    ? "Cena"
                    : "Tipo de comida";
    }

    private sealed class StubMealTypesApiClient : IMealTypesApiClient
    {
        public Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MealTypeResponse>>(
                [new(BreakfastId, "Desayuno", 0), new(LunchId, "Comida", 1), new(DinnerId, "Cena", 2)]);
        public Task<MealTypeResponse> CreateAsync(CreateMealTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<MealTypeResponse> UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubRecipesApiClient : IRecipesApiClient
    {
        public Guid IngredientId { get; } = Guid.NewGuid();
        public Guid UnitTypeId { get; } = Guid.NewGuid();

        public Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecipeListItemResponse>>(
                [
                    new(FirstRecipeId, "Gazpacho", 20),
                    new(SecondRecipeId, "Salmorejo", 25),
                ]);

        public Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(new RecipeResponse(
                id,
                "Gazpacho",
                20,
                [new(Guid.NewGuid(), IngredientId, UnitTypeId, 1m, 0)],
                [],
                [],
                [LunchId]));
        public Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubWeeklyPlanInventoryApiClient : IWeeklyPlanInventoryApiClient
    {
        public MealCompletionResponse Completion { get; init; } =
            new(Guid.NewGuid(), false, [], []);

        public Task<IReadOnlyList<InventoryRequirementResponse>> GetRequirementsAsync(
            Guid planId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InventoryRequirementResponse>>([]);

        public Task<MealCompletionResponse> CompleteMealAsync(
            Guid planId,
            DateOnly mealDate,
            Guid mealTypeId,
            CompleteMealRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(Completion);
    }

    private sealed class StubInventoryApiClient : IInventoryApiClient
    {
        public List<InventoryLotResponse> Items { get; } = [];

        public Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
            bool includeUnavailable,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InventoryLotResponse>>(Items);

        public Task<InventoryLotResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryLotResponse> CreateAsync(CreateInventoryLotRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryLotResponse> CorrectExpirationAsync(Guid id, CorrectInventoryExpirationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryOperationResponse> ConsumeAsync(Guid id, InventoryQuantityRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryOperationResponse> DiscardAsync(Guid id, InventoryQuantityRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryLotResponse> AdjustAsync(Guid id, AdjustInventoryLotRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
