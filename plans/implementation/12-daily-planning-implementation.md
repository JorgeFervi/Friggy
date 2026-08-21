# Implementación ejecutable — Fase 12

- **Fase relacionada:** [Fase 12 — Planificación diaria](../phases/12-daily-planning.md)
- **Skills aplicables:** `architecture`, `modern-csharp`, `entity-framework-core`, `minimal-apis`, `blazor`, `run-tests`
- **Plataforma de pruebas detectada:** .NET SDK 10.0.302, Microsoft Testing Platform y xUnit v3

Esta fase reemplaza el agregado semanal; no lo envuelve ni mantiene rutas de compatibilidad. La dirección de dependencias continúa siendo `Domain <- Application <- Infrastructure <- Api`, y `Friggy.Web` sigue accediendo solo por HTTP.

## Inventario del cambio

| Capa | Estado actual | Destino |
|---|---|---|
| Domain | `WeeklyPlan` contiene siete fechas; hijos guardan `WeeklyPlanId` y `Date` | `DailyPlan` contiene un solo día; hijos guardan `DailyPlanId` |
| Application | `WeeklyPlanService` construye siete días y carga tiempos de receta uno por uno | servicios diarios y lecturas acotadas por intervalo, sin N+1 |
| Infrastructure | `weekly_plans`, índices con plan y fecha | `daily_plans` con fecha única e hijos sin fecha redundante |
| API | `/api/weekly-plans/.../days/{date}` | `/api/daily-plans/{date}/...` |
| Web | lista de semanas y calendario de siete días | explorador de intervalo y editor de un día |
| Inventario | `WeeklyPlanInventoryService` mezcla carencias semanales y finalización | servicio diario de finalización; la comparación multifecha se añade en Fase 14 |

## Matriz de comportamiento

| Criterio | Rojo inicial | Protección adicional |
|---|---|---|
| Un plan como máximo por fecha | Application e Integration | índice único PostgreSQL y `409` |
| Cualquier fecha es válida | Domain | sin regla de lunes ni duración de siete días |
| Tipo de comida único por día | Domain | clave alternativa `(daily_plan_id, meal_type_id)` |
| Orden de huecos | Domain e Integration | índice único `(daily_plan_id, order)` |
| Comensales positivos | Domain | check constraint `servings > 0` |
| Estados irreversibles | Domain | constraints de estado y finalización |
| Finalización idempotente | Application e Integration | una transacción y token de concurrencia |
| Intervalo inclusivo | Application, API y Component | filtro SQL `>= from && <= to` |

## 12.1 — Congelar el comportamiento que se conserva

Antes de renombrar, añadir o ajustar tests que caractericen:

1. Añadir, retirar y reordenar huecos.
2. Asignar o sustituir receta y comensales.
3. Calcular la hora de inicio de preparación, incluidos cruces de medianoche.
4. Omitir una comida y bloquear cambios posteriores.
5. Completar una comida una sola vez y registrar movimientos de inventario.

Estos tests deben comprobar resultados públicos, no llamadas internas. Después se trasladan al namespace `DailyPlans` y dejan de depender de lunes, siete días, nombre o descripción.

## 12.2 — Agregado diario en Domain

La primera prueba roja elimina la restricción semanal y fija la fecha como propiedad del agregado:

```csharp
[Fact]
public void Create_AnyDate_CreatesOneDayPlan()
{
    var date = new DateOnly(2030, 1, 9);

    var plan = DailyPlan.Create(date);

    Assert.NotEqual(Guid.Empty, plan.Id);
    Assert.Equal(date, plan.Date);
    Assert.Empty(plan.Slots);
    Assert.Empty(plan.Entries);
}
```

Modelo orientativo:

```csharp
namespace Friggy.Domain.DailyPlans;

public sealed class DailyPlan
{
    private readonly List<MealPlanEntry> entries = [];
    private readonly List<MealPlanSlot> slots = [];

    private DailyPlan()
    {
    }

    private DailyPlan(Guid id, DateOnly date)
    {
        Id = id;
        Date = date;
    }

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public IReadOnlyList<MealPlanEntry> Entries => entries.AsReadOnly();
    public IReadOnlyList<MealPlanSlot> Slots => slots.AsReadOnly();

    public static DailyPlan Create(DateOnly date) => new(Guid.NewGuid(), date);

    public MealPlanSlot AddSlot(Guid mealTypeId)
    {
        ValidateRequiredId(mealTypeId, "daily-plan.slot.meal-type-id.required");
        if (slots.Any(slot => slot.MealTypeId == mealTypeId))
        {
            throw new DomainValidationException(
                "daily-plan.slot.meal-type.duplicate",
                "El tipo de comida ya existe en este día.");
        }

        var slot = MealPlanSlot.Create(Id, mealTypeId, slots.Count);
        slots.Add(slot);
        return slot;
    }
}
```

Cambios obligatorios en los hijos:

- `WeeklyPlanId` pasa a `DailyPlanId`.
- Se elimina `Date`; la fecha siempre se obtiene de la raíz.
- Las operaciones reciben `mealTypeId` o `slotId`, no una fecha que pueda contradecir al agregado.
- Los códigos cambian de `weekly-plan.*` a `daily-plan.*`.
- `MealPlanEntryStatus` y la identidad `MealPlanEntry.Id` se conservan para mantener finalización e inventario.

Ejemplo de test para evitar fecha duplicada conceptualmente:

```csharp
[Fact]
public void Assign_SameMealTypeTwice_ReplacesWithoutDuplicating()
{
    var plan = DailyPlan.Create(new DateOnly(2030, 1, 9));
    var mealTypeId = Guid.NewGuid();
    var firstRecipeId = Guid.NewGuid();
    var replacementId = Guid.NewGuid();

    plan.Assign(mealTypeId, firstRecipeId, servings: 2);
    plan.Assign(mealTypeId, replacementId, servings: 3);

    var entry = Assert.Single(plan.Entries);
    Assert.Equal(replacementId, entry.RecipeId);
    Assert.Equal(3, entry.Servings);
}
```

## 12.3 — Contratos y casos de uso en Application

Crear `Friggy.Application/DailyPlans` y retirar `Application/WeeklyPlans` cuando todas las referencias estén adaptadas. Contratos mínimos:

```csharp
public sealed record CreateDailyPlanRequest(DateOnly Date);

public sealed record DailyPlanResponse(
    Guid Id,
    DateOnly Date,
    IReadOnlyList<DailyPlanMealResponse> Meals);

public sealed record DailyPlanRangeResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DailyPlanResponse> Plans);
```

El repositorio pertenece a Application porque es un puerto; no se añade otro proyecto ni un repositorio por slot o entrada:

```csharp
public interface IDailyPlanRepository
{
    Task<IReadOnlyList<DailyPlan>> ListBetweenAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<DailyPlan?> GetByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken);

    Task AddAsync(DailyPlan plan, CancellationToken cancellationToken);
    void Remove(DailyPlan plan);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

La consulta valida únicamente el orden del intervalo. No enumera ni materializa días ausentes; Web decide cómo representarlos:

```csharp
public async Task<DailyPlanRangeResponse> ListAsync(
    DateOnly from,
    DateOnly to,
    CancellationToken cancellationToken)
{
    if (from > to)
    {
        throw new DomainValidationException(
            "daily-plan.date-range.invalid",
            "La fecha inicial no puede ser posterior a la final.");
    }

    var plans = await repository.ListBetweenAsync(from, to, cancellationToken);
    return new DailyPlanRangeResponse(
        from,
        to,
        await MapAsync(plans, cancellationToken));
}
```

Al crear:

1. Comprobar si ya existe la fecha y devolver `DailyPlanDateConflictException`.
2. Crear la raíz.
3. Añadir huecos iniciales según los tipos de comida ordenados.
4. Guardar una vez.
5. Traducir también la colisión PostgreSQL, porque la precomprobación no resuelve concurrencia.

Evitar el N+1 actual de `GetRecipeEstimatedTimeAsync` por receta. Añadir una lectura por IDs, por ejemplo `GetRecipeEstimatedTimesAsync(IReadOnlyCollection<Guid> ids, ...)`, y mapear en memoria.

## 12.4 — EF Core y migración PostgreSQL

Configuración objetivo:

```csharp
internal sealed class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable("daily_plans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();
        builder.Property(plan => plan.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .IsRequired();
        builder.HasIndex(plan => plan.Date).IsUnique();
        builder.Navigation(plan => plan.Slots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(plan => plan.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

```csharp
builder.HasAlternateKey(slot => new
{
    slot.DailyPlanId,
    slot.MealTypeId,
});
builder.HasIndex(slot => new { slot.DailyPlanId, slot.Order }).IsUnique();

builder.HasOne<DailyPlan>()
    .WithMany(plan => plan.Slots)
    .HasForeignKey(slot => slot.DailyPlanId)
    .OnDelete(DeleteBehavior.Cascade);
```

La migración `ReplaceWeeklyPlansWithDailyPlans` debe ser nueva; nunca se editan las migraciones 20260809–20260813. Orden recomendado:

1. Retirar temporalmente la FK desde `inventory_movements.meal_plan_entry_id`.
2. Poner a `NULL` las asociaciones existentes, porque se aceptó no migrar datos de planificación.
3. Eliminar `meal_plan_entries`, `meal_plan_slots` y `weekly_plans` en orden seguro.
4. Crear `daily_plans`, `meal_plan_slots` y `meal_plan_entries` con las nuevas claves.
5. Recrear la FK opcional de movimientos hacia `meal_plan_entries` con `Restrict`.
6. Actualizar `FriggyDbContextModelSnapshot` mediante la herramienta de EF, no a mano.

Probar con PostgreSQL real:

- migración desde el último esquema, con un lote y movimientos no vinculados preservados;
- base vacía recorriendo toda la cadena de migraciones;
- dos inserciones concurrentes para la misma fecha;
- round-trip con slots, entradas y estados;
- reordenación sin colisiones temporales de índice;
- borrado aceptado sin completadas y rechazado con movimientos asociados.

El repositorio usa tracking para mutaciones y `AsNoTrackingWithIdentityResolution` o proyección para lecturas. `ListBetweenAsync` filtra antes de `Include`:

```csharp
return await CompleteQuery()
    .AsNoTrackingWithIdentityResolution()
    .Where(plan => plan.Date >= from && plan.Date <= to)
    .OrderBy(plan => plan.Date)
    .ToListAsync(cancellationToken);
```

## 12.5 — Finalización e inventario

Renombrar `WeeklyPlanInventoryService` a `DailyPlanInventoryService` o separar `DailyPlanMealCompletionService` si el servicio deja de ser cohesivo. La operación queda direccionada por fecha:

```csharp
public async Task<MealCompletionResponse> CompleteMealAsync(
    DateOnly date,
    Guid mealTypeId,
    CompleteMealRequest request,
    CancellationToken cancellationToken)
{
    var plan = await plans.GetByDateAsync(date, cancellationToken)
        ?? throw DailyPlanNotFoundException.For(date);
    var entry = plan.GetRequiredEntry(mealTypeId);

    // Cargar receta y lotes, validar y consumir como en el flujo actual.
    plan.CompleteEntry(mealTypeId, timeProvider.GetUtcNow());
    await unitOfWork.SaveChangesAsync(cancellationToken);
    return MapCompletion(entry);
}
```

Mantener:

- una sola instancia scoped de `FriggyDbContext` para plan, lotes y movimientos;
- idempotencia antes de cargar o consumir lotes;
- exclusión de omitidas;
- `InventoryMovement.MealPlanEntryId` como enlace genérico, sin nombres semanales;
- conflicto de concurrencia recuperable.

La carencia visible de un único día puede conservarse temporalmente como consulta diaria. La comparación entre fechas y la lista de compra no se implementan hasta la Fase 14.

## 12.6 — Minimal API

Rutas propuestas:

| Método | Ruta | Resultado |
|---|---|---|
| GET | `/api/daily-plans?from=yyyy-MM-dd&to=yyyy-MM-dd` | planes existentes del intervalo |
| GET | `/api/daily-plans/{date}` | detalle de un día |
| POST | `/api/daily-plans` | crear un día; `409` si ya existe |
| DELETE | `/api/daily-plans/{date}` | borrar un día modificable |
| PUT/DELETE | `/api/daily-plans/{date}/meal-types/{mealTypeId}` | asignar o retirar receta |
| POST | `/api/daily-plans/{date}/slots` | añadir hueco |
| PUT | `/api/daily-plans/{date}/slots/order` | reordenar |
| DELETE | `/api/daily-plans/{date}/slots/{slotId}` | retirar hueco |
| PUT | `/api/daily-plans/{date}/slots/{slotId}/time` | cambiar hora |
| POST | `/api/daily-plans/{date}/meal-types/{mealTypeId}/skip` | omitir |
| POST | `/api/daily-plans/{date}/meal-types/{mealTypeId}/complete` | completar y consumir |

Ejemplo de grupo:

```csharp
public static RouteGroupBuilder MapDailyPlanEndpoints(
    this IEndpointRouteBuilder routes)
{
    var group = routes.MapGroup("/api/daily-plans")
        .WithTags("Daily plans");

    group.MapGet("/", ListAsync)
        .WithName("ListDailyPlans")
        .Produces<DailyPlanRangeResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

    group.MapPost("/", CreateAsync)
        .WithName("CreateDailyPlan")
        .Produces<DailyPlanResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict);

    return group;
}
```

Centralizar el parseo estricto de `DateOnly` para no repetirlo en cada handler. Los endpoints transportan, documentan OpenAPI y delegan; no consultan `DbContext` ni contienen reglas.

## 12.7 — Web Blazor

Sustituciones principales:

| Retirar | Crear |
|---|---|
| `WeeklyPlans.razor` | `DailyPlans.razor` en `/daily-plans` |
| `WeeklyPlanDetails.razor` | `DailyPlanDetails.razor` direccionada por fecha |
| `WeeklyPlanCalendar.razor` | `DailyPlanEditor.razor` presentacional |
| `WeeklyPlanApiClient` | `DailyPlanApiClient` |
| `WeeklyPlanFormModel` | `DailyPlanRangeFormModel` y solicitud de creación por fecha |

La página contenedora carga planes, recetas y tipos de comida. Un componente presentacional recibe un `DailyPlanResponse` inmutable y emite callbacks:

```razor
@foreach (var plan in Model.Plans)
{
    <DailyPlanEditor @key="plan.Date"
                     Model="plan"
                     Recipes="Recipes"
                     MealTypes="MealTypes"
                     OnChanged="HandleChangeAsync" />
}
```

La experiencia debe distinguir:

- fecha sin `DailyPlan`: acción “Planificar este día”;
- plan creado sin recetas: huecos pendientes;
- entrada planificada, omitida o completada;
- intervalo vacío, carga, error recuperable y guardado en curso.

No mutar directamente los DTO recibidos como parámetros. Después de cada escritura correcta, sustituir el modelo por la respuesta HTTP o recargar el intervalo. Conservar `@key`, etiquetas accesibles, cancelación al disponer y bloqueo contra doble envío.

## 12.8 — Limpieza de referencias semanales

La limpieza incluye código compilado, tests, navegación, textos, CSS, baselines y documentación vigente:

```powershell
rg -n "WeeklyPlan|WeeklyPlans|weekly-plan|weekly-plans|plan semanal|planes semanales" `
  src tests docs README.md CONTRIBUTING.md
```

Revisar cada coincidencia. Excepciones deliberadas:

- migraciones y designers históricos anteriores a la Fase 12;
- `plans/completed/`, que conserva el registro de lo ejecutado;
- el propio plan de sustitución mientras siga siendo documentación futura/histórica.

Actualizar al completar:

- `docs/product/current-capabilities.md` y `roadmap.md`;
- `docs/development/architecture.md`, `api.md` y recorridos técnicos;
- guías de usuario y primeros pasos;
- navegación y textos de Home;
- nombres de pruebas y helpers de reset;
- baselines `weekly-*`, reemplazándolos solo después de revisar `actual` y `diff`.

No ejecutar `update-visual-baselines.ps1 -Accept` para ocultar una regresión sin explicar.

## 12.9 — Secuencia TDD y gate

Ejecutar primero el rojo focalizado, después la suite de la capa y finalmente el gate. En .NET 10 + MTP los argumentos xUnit se pasan sin separador `--`:

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.DailyPlans.DailyPlanTests"

dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.DailyPlans.DailyPlanServiceTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Persistence.DailyPlanRepositoryTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.DailyPlans.DailyPlanPageTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-class "Friggy.EndToEndTests.DailyPlanJourneyTests"
```

Gate final:

```powershell
./scripts/quality-gate.ps1
```

## Cierre

- [ ] `DailyPlan` es la única raíz de planificación activa.
- [ ] La unicidad por fecha está cubierta en Domain/Application y PostgreSQL.
- [ ] Finalización e inventario siguen siendo atómicos e idempotentes.
- [ ] No hay N+1 de tiempos de receta en el detalle/rango.
- [ ] API y Web no conservan rutas ni contratos semanales.
- [ ] La migración destructiva está documentada y probada desde dos orígenes.
- [ ] Capturas responsive revisadas en móvil, tableta y escritorio.
- [ ] Build, formato, pruebas, auditoría y documentación están verdes.
