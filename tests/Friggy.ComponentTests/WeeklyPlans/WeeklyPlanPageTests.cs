using Bunit;
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
        JavaScript.Setup<bool>("confirm", _ => true).SetResult(true);
        var component = Render<global::Friggy.Web.Components.Pages.WeeklyPlans>();
        component.WaitForElement("button[data-action='delete-weekly-plan']");

        component.Find("button[data-action='delete-weekly-plan']").Click();

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

    private void RegisterApis(
        StubWeeklyPlansApiClient weeklyPlans,
        StubRecipesApiClient? recipes = null,
        StubWeeklyPlanInventoryApiClient? weeklyPlanInventory = null,
        StubInventoryApiClient? inventory = null)
    {
        Services.AddSingleton<IWeeklyPlansApiClient>(weeklyPlans);
        Services.AddSingleton<IRecipesApiClient>(recipes ?? new StubRecipesApiClient());
        Services.AddSingleton<IWeeklyPlanInventoryApiClient>(
            weeklyPlanInventory ?? new StubWeeklyPlanInventoryApiClient());
        Services.AddSingleton<IInventoryApiClient>(inventory ?? new StubInventoryApiClient());
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
                        new(BreakfastId, "Desayuno", 0, null),
                        new(LunchId, "Comida", 1, null),
                        new(DinnerId, "Cena", 2, null),
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
