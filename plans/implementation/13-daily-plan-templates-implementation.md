# Implementación ejecutable — Fase 13

- **Fase relacionada:** [Fase 13 — Plantillas de planes diarios](../phases/13-daily-plan-templates.md)
- **Dependencia ejecutable:** gate verde de la Fase 12
- **Estado:** implementación presente; checklist, E2E/visual y gate final pendientes
- **Skills aplicables:** `architecture`, `modern-csharp`, `entity-framework-core`, `minimal-apis`, `blazor`, `run-tests`

La plantilla es una raíz independiente y reutilizable. Aplicarla materializa planes diarios nuevos; no mantiene relaciones con ellos ni participa en finalización o inventario.

> **Evidencia actual:** existen `DailyPlanTemplateTests`, `DailyPlanTemplateServiceTests`, `DailyPlanTemplateEndpointTests` y `DailyPlanTemplatesApiClientTests`. No existen todavía las clases previstas `DailyPlanTemplateRepositoryTests`, `DailyPlanTemplatePageTests` ni `DailyPlanTemplateJourneyTests`; los comandos que las mencionan permanecen como trabajo pendiente y no como evidencia ejecutada.

## Matriz de comportamiento

| Criterio | Rojo inicial | Protección adicional |
|---|---|---|
| Nombre obligatorio y único | Domain y Application | índice único normalizado |
| Tipo de comida único | Domain | clave alternativa por plantilla y tipo |
| Orden continuo y no negativo | Domain | índice único y check PostgreSQL |
| Receta opcional | Domain y Application | FK restrict cuando existe |
| Comensales positivos | Domain | check constraint |
| Copia con IDs nuevos | Domain | Integration round-trip |
| Fechas explícitas y únicas | Application | validación antes de escribir |
| No sobrescribir planes | Application e Integration | índice único de `DailyPlan.Date` |
| Aplicación atómica | Integration | una sola unidad de trabajo |

## 13.1 — Modelo de plantilla en Domain

Empezar por el comportamiento de copia, porque es el valor principal y evita diseñar la entidad como un simple DTO persistido:

```csharp
[Fact]
public void Instantiate_ConfiguredTemplate_CreatesIndependentDailyPlan()
{
    var template = DailyPlanTemplate.Create("Día de entrenamiento");
    var mealTypeId = Guid.NewGuid();
    var recipeId = Guid.NewGuid();
    template.AddMeal(mealTypeId, recipeId, servings: 2, new TimeOnly(14, 30));

    var first = template.Instantiate(new DateOnly(2030, 1, 8));
    var second = template.Instantiate(new DateOnly(2030, 1, 10));

    Assert.NotEqual(first.Id, second.Id);
    Assert.NotEqual(first.Slots.Single().Id, second.Slots.Single().Id);
    Assert.NotEqual(first.Entries.Single().Id, second.Entries.Single().Id);
    Assert.Equal(2, first.Entries.Single().Servings);
}
```

Modelo orientativo:

```csharp
namespace Friggy.Domain.DailyPlanTemplates;

public sealed class DailyPlanTemplate
{
    private readonly List<DailyPlanTemplateMeal> meals = [];

    private DailyPlanTemplate()
    {
        Name = null!;
    }

    private DailyPlanTemplate(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }
    public CatalogName Name { get; private set; }
    public IReadOnlyList<DailyPlanTemplateMeal> Meals => meals.AsReadOnly();

    public static DailyPlanTemplate Create(string? name) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "daily-plan-template.name.required"));

    public DailyPlan Instantiate(DateOnly date)
    {
        var plan = DailyPlan.Create(date);
        foreach (var meal in meals.OrderBy(item => item.Order))
        {
            var slot = plan.AddSlot(meal.MealTypeId);
            plan.SetSlotTime(slot.Id, meal.PlannedTime);
            if (meal.RecipeId.HasValue)
            {
                plan.Assign(meal.MealTypeId, meal.RecipeId.Value, meal.Servings);
            }
        }

        return plan;
    }
}
```

`DailyPlanTemplateMeal` contiene:

- `Id` y `DailyPlanTemplateId`;
- `MealTypeId`;
- `RecipeId?`;
- `Servings`, siempre mayor que cero y con valor inicial uno;
- `PlannedTime?`;
- `Order` no negativo.

No contiene fecha, `MealPlanEntryStatus`, motivo de omisión, alternativa ni finalización. Añadir rojos para duplicado de tipo, orden inválido, receta vacía (`Guid.Empty`), raciones no positivas, reordenación y retirada.

## 13.2 — Application y referencias

DTO mínimos:

```csharp
public sealed record DailyPlanTemplateMealRequest(
    Guid MealTypeId,
    Guid? RecipeId,
    int Servings,
    string? PlannedTime,
    int Order);

public sealed record CreateDailyPlanTemplateRequest(
    string Name,
    IReadOnlyList<DailyPlanTemplateMealRequest> Meals);

public sealed record ApplyDailyPlanTemplateRequest(
    IReadOnlyList<DateOnly> Dates);

public sealed record ApplyDailyPlanTemplateResponse(
    Guid TemplateId,
    IReadOnlyList<DailyPlanResponse> CreatedPlans);
```

Crear `IDailyPlanTemplateRepository` para la raíz. Reutilizar o generalizar el puerto de referencias de planificación para comprobar recetas y tipos; no crear un repositorio por `DailyPlanTemplateMeal`.

La creación/edición valida todas las referencias antes de modificar el agregado. Usar lectura en lote:

```csharp
var mealTypeIds = request.Meals.Select(meal => meal.MealTypeId).ToHashSet();
var recipeIds = request.Meals
    .Where(meal => meal.RecipeId.HasValue)
    .Select(meal => meal.RecipeId!.Value)
    .ToHashSet();

await references.EnsureExistAsync(
    mealTypeIds,
    recipeIds,
    cancellationToken);
```

Evitar un `AnyAsync` por fila. El resultado debe identificar de forma estable los IDs inexistentes mediante `daily-plan-template.meal-type.not-found` o `daily-plan-template.recipe.not-found`.

## 13.3 — Aplicación a fechas

Reglas previas, en este orden:

1. La colección no puede ser nula ni vacía.
2. Ninguna fecha puede repetirse.
3. La plantilla debe existir.
4. Ninguna fecha puede tener ya un `DailyPlan`.
5. Todas las referencias de la plantilla deben seguir existiendo.
6. Se crean todos los planes y se guarda una sola vez.

Ejemplo orientativo:

```csharp
public async Task<ApplyDailyPlanTemplateResponse> ApplyAsync(
    Guid templateId,
    ApplyDailyPlanTemplateRequest request,
    CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Dates);

    var dates = request.Dates.ToArray();
    if (dates.Length == 0 || dates.Distinct().Count() != dates.Length)
    {
        throw new DomainValidationException(
            "daily-plan-template.dates.invalid",
            "Indica al menos una fecha y no la repitas.");
    }

    var template = await templates.GetByIdAsync(templateId, cancellationToken)
        ?? throw DailyPlanTemplateNotFoundException.For(templateId);
    var conflicts = await dailyPlans.ListExistingDatesAsync(dates, cancellationToken);
    if (conflicts.Count > 0)
    {
        throw DailyPlanTemplateConflictException.ForDates(conflicts);
    }

    var plans = dates
        .OrderBy(date => date)
        .Select(template.Instantiate)
        .ToArray();
    await dailyPlans.AddRangeAsync(plans, cancellationToken);
    await dailyPlans.SaveChangesAsync(cancellationToken);

    return new ApplyDailyPlanTemplateResponse(
        template.Id,
        await mappings.ToResponsesAsync(plans, cancellationToken));
}
```

`AddRangeAsync` no hace `SaveChangesAsync` por elemento. El repositorio usa el mismo `FriggyDbContext` scoped y guarda una sola vez. Infrastructure traduce una colisión concurrente de fecha a `409`; todo el lote debe quedar revertido.

Tests con fakes manuales:

- colección nula, vacía y duplicada;
- plantilla inexistente;
- una o varias fechas ya planificadas, comprobando cero altas;
- fechas sueltas ordenadas en la respuesta;
- cancelación antes de guardar;
- copia de hueco vacío, hora, receta y comensales;
- fallo al guardar sin estado parcial observable.

## 13.4 — Persistencia y migración

Tablas propuestas:

```text
daily_plan_templates
  Id uuid PK
  name text
  normalized_name text UNIQUE

daily_plan_template_meals
  Id uuid PK
  daily_plan_template_id uuid FK CASCADE
  meal_type_id uuid FK RESTRICT
  recipe_id uuid NULL FK RESTRICT
  servings integer CHECK > 0
  planned_time time without time zone NULL
  order integer CHECK >= 0
```

Índices y claves:

```csharp
builder.HasAlternateKey(meal => new
{
    meal.DailyPlanTemplateId,
    meal.MealTypeId,
});

builder.HasIndex(meal => new
{
    meal.DailyPlanTemplateId,
    meal.Order,
}).IsUnique();
```

Usar `CatalogConfiguration.ConfigureName` para conservar las reglas de nombre del proyecto. La FK de receta es opcional pero restrict cuando existe. El borrado de una plantilla elimina sus comidas; no afecta a ningún `DailyPlan` porque no hay vínculo.

Pruebas PostgreSQL:

- migración desde Fase 12 y desde base vacía;
- nombre normalizado duplicado;
- tipo y orden duplicados;
- receta nula aceptada y receta inexistente rechazada;
- round-trip de `TimeOnly` como `time without time zone`;
- aplicación de varias fechas en una transacción;
- carrera entre dos aplicaciones que contienen la misma fecha.

## 13.5 — Minimal API

Rutas:

| Método | Ruta | Semántica |
|---|---|---|
| GET | `/api/daily-plan-templates` | listado |
| GET | `/api/daily-plan-templates/{id}` | detalle |
| POST | `/api/daily-plan-templates` | crear |
| PUT | `/api/daily-plan-templates/{id}` | reemplazar configuración editable |
| DELETE | `/api/daily-plan-templates/{id}` | borrar |
| POST | `/api/daily-plan-templates/{id}/apply` | crear planes para fechas explícitas |

```csharp
group.MapPost("/{id:guid}/apply", ApplyAsync)
    .WithName("ApplyDailyPlanTemplate")
    .Produces<ApplyDailyPlanTemplateResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status409Conflict);
```

La respuesta `409` debe incluir `extensions["code"]` y `extensions["conflictingDates"]`. No devolver entidades de Domain. Propagar `CancellationToken` en todos los handlers y reflejar solicitudes/respuestas en OpenAPI.

## 13.6 — Web Blazor

Añadir navegación “Plantillas” bajo planificación y una página `/daily-plan-templates` con:

- listado, vacío, carga y error;
- formulario de nombre;
- filas ordenables para tipos de comida;
- receta opcional, comensales y hora;
- edición y eliminación con confirmación;
- diálogo de aplicación con fechas explícitas.

Separar:

```text
DailyPlanTemplates.razor          contenedor, HTTP y feedback
DailyPlanTemplateForm.razor      editor presentacional
DailyPlanTemplateMealRow.razor   una comida
ApplyDailyPlanTemplateDialog     selección de fechas y confirmación
```

Ejemplo de comunicación inmutable:

```razor
@foreach (var meal in Meals.OrderBy(item => item.Order))
{
    <DailyPlanTemplateMealRow @key="meal.ClientId"
                              Model="meal"
                              ModelChanged="UpdateMealAsync" />
}
```

Usar un `ClientId` solo en el formulario para mantener `@key` antes de persistir; no enviarlo a la API. El diálogo conserva las fechas elegidas si la API devuelve conflicto, presenta todas las fechas ocupadas y no ofrece sobrescritura.

Pruebas bUnit:

- validación de nombre y raciones;
- receta opcional;
- añadir, retirar y reordenar comidas;
- bloqueo durante envío;
- conflicto conserva borrador y fechas;
- éxito muestra planes creados y permite navegar al primero;
- errores HTTP visibles, sin tragarlos.

## 13.7 — Recorrido E2E y visual

Recorrido mínimo:

1. Crear recetas y una plantilla con un hueco vacío y otro asignado.
2. Aplicarla a dos fechas no contiguas.
3. Verificar que ambos planes tienen configuración equivalente e IDs distintos.
4. Editar la plantilla y comprobar que los planes anteriores no cambian.
5. Intentar aplicarla a una fecha ocupada junto a otra libre y comprobar que no se crea la libre.
6. Reiniciar servicios y verificar persistencia.

Capturas revisadas:

- listado vacío y con plantillas;
- formulario móvil y escritorio;
- diálogo de fechas;
- conflicto accesible.

## 13.8 — Secuencia TDD y gate

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.DailyPlanTemplates.DailyPlanTemplateTests"

dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.DailyPlanTemplates.DailyPlanTemplateServiceTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Persistence.DailyPlanTemplateRepositoryTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.DailyPlanTemplates.DailyPlanTemplatePageTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-class "Friggy.EndToEndTests.DailyPlanTemplateJourneyTests"

./scripts/quality-gate.ps1
```

## Cierre

- [ ] Plantilla y comidas protegen sus invariantes en Domain.
- [ ] No existe relación persistente plantilla → plan diario.
- [ ] Referencias se validan en lote y no hay N+1.
- [ ] La aplicación multifecha es atómica y nunca sobrescribe.
- [ ] La concurrencia por fecha produce conflicto recuperable.
- [ ] API, cliente y UI conservan borradores ante error.
- [ ] Migración, PostgreSQL, bUnit, E2E, visual y gate están verdes.
