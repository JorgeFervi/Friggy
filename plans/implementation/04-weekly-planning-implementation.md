# Implementación ejecutable — Fase 4

- **Fase relacionada:** [Fase 4 — Planificación semanal](../phases/04-weekly-planning.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-data`, `dotnet-frontend`, `code-testing-planner`, `test-quality-auditor`, `dotnet-review`
- **Skills instaladas:** `architecture`, `modern-csharp`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`, `assertion-quality`, `test-anti-patterns`

`WeeklyPlan` es la raíz del agregado y controla `MealPlanEntry`. La semana comienza el lunes, contiene exactamente siete fechas y solo admite una receta por combinación de fecha y tipo de comida.

## Matriz de comportamiento

| Criterio | Rojo inicial | Protección adicional |
|---|---|---|
| Inicio en lunes | Domain | código estable de error |
| Siete días exactos | Domain y Application | DTO siempre devuelve siete días |
| Fecha dentro de la semana | Domain | constraint lógico y prueba de API |
| Entrada única | Domain e Integration | índice compuesto único |
| Asignar/sustituir/retirar | Domain, Application y Component | endpoint idempotente por celda |

## 4.1 — Crear la semana en Domain — Completada

```csharp
[Fact]
public void Create_StartDateIsNotMonday_ThrowsDomainValidationException()
{
    var tuesday = new DateOnly(2026, 8, 4);

    var exception = Assert.Throws<DomainValidationException>(
        () => WeeklyPlan.Create("Semana 32", tuesday, null));

    Assert.Equal("weekly-plan.start-date.monday", exception.Code);
}
```

Implementación mínima orientativa:

```csharp
public sealed class WeeklyPlan
{
    private readonly List<MealPlanEntry> entries = [];

    public DateOnly StartDate { get; }
    public DateOnly EndDate => StartDate.AddDays(6);
    public IReadOnlyCollection<MealPlanEntry> Entries => entries.AsReadOnly();

    public static WeeklyPlan Create(string name, DateOnly startDate, string? description)
    {
        if (startDate.DayOfWeek is not DayOfWeek.Monday)
            throw DomainValidationException.WithCode(
                "weekly-plan.start-date.monday");

        return new WeeklyPlan(Guid.NewGuid(), name, startDate, description);
    }
}
```

Añadir tests para nombre, fecha final, enumeración de siete fechas e identidad. No persistir `EndDate` si siempre se deriva sin ambigüedad.

## 4.2 — Asignar, sustituir y retirar — Completada

El comportamiento de la celda es idempotente: asignar la misma receta dos veces no duplica; asignar otra sustituye; retirar una celda inexistente no afecta a otras entradas.

```csharp
[Fact]
public void Assign_ExistingSlot_ReplacesRecipeWithoutDuplicatingEntry()
{
    var plan = WeeklyPlan.Create("Semana", new DateOnly(2026, 8, 3), null);
    var mealTypeId = Guid.NewGuid();
    var firstRecipe = Guid.NewGuid();
    var replacement = Guid.NewGuid();

    plan.Assign(new DateOnly(2026, 8, 4), mealTypeId, firstRecipe);
    plan.Assign(new DateOnly(2026, 8, 4), mealTypeId, replacement);

    var entry = Assert.Single(plan.Entries);
    Assert.Equal(replacement, entry.RecipeId);
}
```

Antes de implementar, añadir rojos para fecha fuera de rango e IDs inválidos. La colección solo cambia después de que todas las validaciones hayan pasado.

## 4.3 — Application

Casos de uso mínimos: crear/listar/obtener/actualizar/borrar semana, asignar o retirar una receta y devolver siete días incluso vacíos, ordenados por fecha y tipo de comida.

```csharp
public sealed record SetMealPlanEntryRequest(Guid RecipeId);

public async Task<WeeklyPlanResponse> ExecuteAsync(
    Guid planId,
    DateOnly date,
    Guid mealTypeId,
    SetMealPlanEntryRequest request,
    CancellationToken cancellationToken)
{
    var plan = await plans.GetRequiredAsync(planId, cancellationToken);
    await references.EnsureRecipeAndMealTypeExistAsync(
        request.RecipeId, mealTypeId, cancellationToken);

    plan.Assign(date, mealTypeId, request.RecipeId);
    await unitOfWork.SaveChangesAsync(cancellationToken);
    return WeeklyPlanMappings.ToResponse(plan);
}
```

Probar no encontrado, referencia inexistente, fecha fuera de rango, cancelación y estado final del fake. No duplicar las invariantes de Domain dentro del handler.

## 4.4 — Persistencia y migración

```csharp
internal sealed class MealPlanEntryConfiguration
    : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("meal_plan_entries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.WeeklyPlanId, x.Date, x.MealTypeId })
            .IsUnique();
        builder.HasOne<Recipe>().WithMany().HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MealType>().WithMany().HasForeignKey(x => x.MealTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

La migración debe reflejar índice compuesto y FK restrict. Probar con PostgreSQL real: colisión concurrente, sustitución atómica y lectura ordenada desde un nuevo scope.

## 4.5 — API idempotente por celda

```csharp
group.MapPut("/{planId:guid}/days/{date}/meal-types/{mealTypeId:guid}", SetEntryAsync)
    .WithName("SetMealPlanEntry")
    .Produces<WeeklyPlanResponse>()
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);

group.MapDelete("/{planId:guid}/days/{date}/meal-types/{mealTypeId:guid}", RemoveEntryAsync)
    .WithName("RemoveMealPlanEntry");
```

Documentar `DateOnly` como `yyyy-MM-dd`. Tests previos: PUT nuevo, PUT sustitución, DELETE, fecha inválida/fuera de semana, IDs inexistentes, `ProblemDetails` y lectura posterior.

## 4.6 — Calendario Blazor y bUnit

```razor
@foreach (var day in Model.Days)
{
    <section @key="day.Date" aria-label="@day.AccessibleLabel">
        <h2>@day.DisplayName</h2>
        @foreach (var meal in day.Meals)
        {
            <MealPlanCell @key="meal.MealTypeId" Day="day.Date"
                          Model="meal" OnChanged="ReloadAsync" />
        }
    </section>
}
```

```csharp
[Fact]
public void Render_EmptyWeek_ShowsSevenDaysAndEmptySlots()
{
    using var context = WeeklyPlanTestContext.CreateEmptyWeek();
    var component = context.Render<WeeklyPlanCalendar>();

    Assert.Equal(7, component.FindAll("section[aria-label]").Count);
    Assert.All(component.FindAll("[data-testid='meal-slot']"), slot =>
        Assert.Contains("Sin receta", slot.TextContent));
}
```

Cubrir loading, error, asignación, sustitución y retirada. Preferir rol, label o `data-testid` semántico a selectores visuales.

## 4.7 — Gate y handoff

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.WeeklyPlans.WeeklyPlanTests"
dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj
dotnet test --solution Friggy.sln
```

Ejecutar además un E2E vertical: crear semana, asignar receta y comprobar día/comida. Cero sleeps o datos compartidos. Después habilitar [Fase 5](../phases/05-full-integration.md).
