# Implementación ejecutable — Fase 14

- **Fase relacionada:** [Fase 14 — Lista de la compra calculada](../phases/14-shopping-list.md)
- **Dependencia ejecutable:** modelo diario y gate verde de la Fase 12
- **Estado:** implementación y recorridos presentes; checklist y gate final no registrados
- **Skills aplicables:** `architecture`, `modern-csharp`, `entity-framework-core`, `minimal-apis`, `blazor`, `run-tests`

La lista de la compra es un read model calculado. No se añade un agregado, repositorio de escritura, tabla, migración ni `SaveChangesAsync`.

> **Evolución posterior:** esta guía conserva la semántica exacta con la que se implementó la fase. La [Fase 16](../../docs/plans/phases/16-unit-conversions.md) sustituyó la agrupación pública por dimensión compatible y unidad de compra determinista, sin convertir la lista en una entidad persistida.

## Semántica cerrada

| Entrada | Regla |
|---|---|
| Intervalo | `from` y `to` obligatorios e inclusivos; `from <= to` |
| Planes | solo `DailyPlan.Date` dentro del intervalo |
| Comidas | solo `MealPlanEntryStatus.Planned` y con receta |
| Necesidad | `RecipeIngredient.Quantity * MealPlanEntry.Servings` |
| Agregación | clave exacta `(IngredientId, UnitTypeId)` |
| Inventario | cantidad positiva y `ExpirationDate >= Today` |
| Compra | `Math.Max(0, Required - Available)` |
| Efectos | ninguno; no se consume, reserva ni persiste |

“Hoy” procede de `TimeProvider.GetLocalNow()` para conservar el comportamiento local monousuario y permitir tests deterministas. Una caducidad futura dentro del intervalo no se pronostica en esta versión.

## Matriz de riesgos

| Riesgo | Protección principal |
|---|---|
| Sumar mal comensales | Application con ejemplos de varias recetas y días |
| Mezclar unidades | clave compuesta y pruebas de unidades distintas |
| Descontar stock varias veces | inventario agregado una vez antes del join |
| Contar omitidas/completadas | filtro de estado en SQL e Integration |
| N+1 o carga completa | proyección agrupada en Infrastructure y revisión de SQL |
| Alterar inventario al consultar | contexto no tracking y test de estado antes/después |
| Intervalo enorme | filtro/agrupación en PostgreSQL, cancelación y medición focalizada |

## 14.1 — Contratos de Application

Crear una capacidad `Friggy.Application/ShoppingLists`:

```csharp
public sealed record ShoppingListResponse(
    DateOnly From,
    DateOnly To,
    DateOnly CalculatedOn,
    IReadOnlyList<ShoppingListItemResponse> Items);

public sealed record ShoppingListItemResponse(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RequiredQuantity,
    decimal AvailableQuantity,
    decimal QuantityToBuy);
```

Contratos internos del read model:

```csharp
public sealed record PlannedIngredientDemand(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    string UnitTypeName,
    string UnitSymbol,
    decimal RequiredQuantity);

public sealed record AvailableIngredientStock(
    Guid IngredientId,
    Guid UnitTypeId,
    decimal AvailableQuantity);

public sealed record ShoppingListSnapshot(
    IReadOnlyList<PlannedIngredientDemand> Demands,
    IReadOnlyList<AvailableIngredientStock> Stock);

public interface IShoppingListReadRepository
{
    Task<ShoppingListSnapshot> GetSnapshotAsync(
        DateOnly from,
        DateOnly to,
        DateOnly today,
        CancellationToken cancellationToken);
}
```

Este puerto representa una consulta transversal y evita forzar `IDailyPlanRepository`, `IRecipeRepository` e `IInventoryLotRepository` a materializar agregados completos. No es un repositorio genérico ni uno nuevo por entidad.

## 14.2 — Servicio y cálculo

Primera prueba roja:

```csharp
[Fact]
public async Task GetAsync_RepeatedRequirementAndPartialStock_ReturnsAggregatedPurchase()
{
    var ingredientId = Guid.NewGuid();
    var unitTypeId = Guid.NewGuid();
    var repository = new FakeShoppingListReadRepository
    {
        Snapshot = new(
            [new(ingredientId, "Tomate", unitTypeId, "Gramo", "g", 750m)],
            [new(ingredientId, unitTypeId, 200m)]),
    };
    var service = new ShoppingListService(repository, FrozenTime.At(2030, 1, 5));

    var result = await service.GetAsync(
        new DateOnly(2030, 1, 6),
        new DateOnly(2030, 1, 8),
        TestContext.Current.CancellationToken);

    var item = Assert.Single(result.Items);
    Assert.Equal(750m, item.RequiredQuantity);
    Assert.Equal(200m, item.AvailableQuantity);
    Assert.Equal(550m, item.QuantityToBuy);
}
```

Implementación mínima:

```csharp
public sealed class ShoppingListService(
    IShoppingListReadRepository repository,
    TimeProvider timeProvider)
{
    public async Task<ShoppingListResponse> GetAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw new DomainValidationException(
                "shopping-list.date-range.invalid",
                "La fecha inicial no puede ser posterior a la final.");
        }

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var snapshot = await repository.GetSnapshotAsync(
            from,
            to,
            today,
            cancellationToken);
        var stock = snapshot.Stock.ToDictionary(
            item => (item.IngredientId, item.UnitTypeId),
            item => item.AvailableQuantity);

        var items = snapshot.Demands
            .Select(demand =>
            {
                var available = stock.GetValueOrDefault(
                    (demand.IngredientId, demand.UnitTypeId));
                return new ShoppingListItemResponse(
                    demand.IngredientId,
                    demand.IngredientName,
                    demand.UnitTypeId,
                    demand.UnitTypeName,
                    demand.UnitSymbol,
                    demand.RequiredQuantity,
                    available,
                    Math.Max(0, demand.RequiredQuantity - available));
            })
            .OrderBy(item => item.IngredientName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.UnitTypeName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new ShoppingListResponse(from, to, today, items);
    }
}
```

El fake devuelve datos ya agregados y permite probar Application sin EF Core. Cubrir:

- `from > to` sin consultar el puerto;
- un solo día y extremos inclusivos;
- cero demandas;
- stock cero, parcial, exacto y superior;
- mismo ingrediente con dos unidades, en líneas separadas;
- orden estable por ingrediente/unidad;
- propagación de cancelación.

## 14.3 — Read model EF Core

Implementar `ShoppingListReadRepository` con dos consultas secuenciales dentro del mismo `DbContext` scoped. No usar `Task.WhenAll`: EF Core no admite operaciones concurrentes sobre la misma instancia.

Proyección orientativa de demanda:

```csharp
var demands = await context.DailyPlans
    .AsNoTracking()
    .Where(plan => plan.Date >= from && plan.Date <= to)
    .SelectMany(
        plan => plan.Entries.Where(entry =>
            entry.Status == MealPlanEntryStatus.Planned),
        (_, entry) => entry)
    .Join(
        context.Set<RecipeIngredient>(),
        entry => entry.RecipeId,
        line => line.RecipeId,
        (entry, line) => new { entry.Servings, Line = line })
    .Join(
        context.Ingredients,
        item => item.Line.IngredientId,
        ingredient => ingredient.Id,
        (item, ingredient) => new { item.Servings, item.Line, Ingredient = ingredient })
    .Join(
        context.UnitTypes,
        item => item.Line.UnitTypeId,
        unit => unit.Id,
        (item, unit) => new { item.Servings, item.Line, item.Ingredient, Unit = unit })
    .GroupBy(item => new
    {
        item.Ingredient.Id,
        IngredientName = item.Ingredient.Name.Value,
        UnitTypeId = item.Unit.Id,
        UnitTypeName = item.Unit.Name.Value,
        item.Unit.Symbol,
    })
    .Select(group => new PlannedIngredientDemand(
        group.Key.Id,
        group.Key.IngredientName,
        group.Key.UnitTypeId,
        group.Key.UnitTypeName,
        group.Key.Symbol,
        group.Sum(item => item.Line.Quantity * item.Servings)))
    .ToListAsync(cancellationToken);
```

Proyección de inventario:

```csharp
var stock = await context.InventoryLots
    .AsNoTracking()
    .Where(lot => lot.Quantity > 0 && lot.ExpirationDate >= today)
    .GroupBy(lot => new { lot.IngredientId, lot.UnitTypeId })
    .Select(group => new AvailableIngredientStock(
        group.Key.IngredientId,
        group.Key.UnitTypeId,
        group.Sum(lot => lot.Quantity)))
    .ToListAsync(cancellationToken);
```

Si la proyección sobre `CatalogName.Value` no se traduce con Npgsql, ajustar la proyección EF sin materializar entidades completas. Verificar `ToQueryString()` y el SQL observado en Integration; no mover `GroupBy` a memoria como atajo.

Índices:

- reutilizar el índice único de `daily_plans.date`;
- reutilizar la clave/índice de entradas por `daily_plan_id`;
- verificar el índice de lotes por ingrediente, unidad y caducidad;
- añadir un índice nuevo solo si `EXPLAIN` o la medición muestra un scan evitable, no de forma especulativa.

## 14.4 — Integración con PostgreSQL

Escenarios obligatorios:

1. Dos días en los extremos del intervalo y uno fuera.
2. Dos recetas con el mismo ingrediente/unidad y comensales diferentes.
3. Dos líneas equivalentes dentro de una receta.
4. Mismo ingrediente en unidades distintas.
5. Hueco vacío, entrada omitida y entrada completada excluidos.
6. Lotes agotado, caducado ayer, válido hoy y válido mañana.
7. Stock superior a necesidad, produciendo `QuantityToBuy = 0`.
8. Cero planes o cero recetas, produciendo colección vacía.
9. Consulta antes y después con lotes y movimientos idénticos para demostrar ausencia de escritura.

Ejemplo de aserción de estados:

```csharp
[Fact]
public async Task GetSnapshotAsync_CompletedAndSkippedMeals_ExcludesBoth()
{
    // Arrange: tres planes/entradas equivalentes, una Planned, una Completed y una Skipped.

    var snapshot = await repository.GetSnapshotAsync(
        new DateOnly(2030, 1, 1),
        new DateOnly(2030, 1, 31),
        new DateOnly(2030, 1, 1),
        TestContext.Current.CancellationToken);

    var demand = Assert.Single(snapshot.Demands);
    Assert.Equal(1m, demand.RequiredQuantity);
}
```

## 14.5 — Minimal API y errores

Ruta única:

```text
GET /api/shopping-list?from=2030-01-06&to=2030-01-12
```

```csharp
public static RouteGroupBuilder MapShoppingListEndpoints(
    this IEndpointRouteBuilder routes)
{
    var group = routes.MapGroup("/api/shopping-list")
        .WithTags("Shopping list");

    group.MapGet("/", GetAsync)
        .WithName("GetShoppingList")
        .WithDescription(
            "Calcula la comparación inclusiva. Las fechas usan yyyy-MM-dd.")
        .Produces<ShoppingListResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

    return group;
}
```

El handler debe:

- exigir ambos parámetros;
- parsearlos estrictamente como `yyyy-MM-dd`;
- devolver `shopping-list.date.required`, `shopping-list.date.format` o `shopping-list.date-range.invalid`;
- pasar `CancellationToken`;
- delegar todo cálculo en Application.

Ejemplo de firma tipada:

```csharp
private static async Task<Results<Ok<ShoppingListResponse>, ProblemHttpResult>> GetAsync(
    string? from,
    string? to,
    ShoppingListService service,
    CancellationToken cancellationToken)
```

Integration HTTP comprueba 200, parámetros ausentes, formato inválido, intervalo inverso, OpenAPI y que una segunda consulta tras modificar un lote refleja el nuevo inventario.

## 14.6 — Cliente y página Blazor

Añadir `IShoppingListApiClient` y `ShoppingListApiClient` sin compartir estado global. La página `/shopping-list` contiene:

- fecha inicial y final;
- botón “Calcular lista”;
- indicación “Inventario calculado a fecha …”;
- filas con ingrediente, unidad, necesario, disponible y a comprar;
- estado “Cubierto” cuando `QuantityToBuy == 0`;
- vacíos distintos para “no hay comidas planificadas” y resultado cargando/error.

Formulario orientativo:

```razor
<EditForm Model="range" OnValidSubmit="CalculateAsync" FormName="shopping-list-range">
    <DataAnnotationsValidator />
    <ValidationSummary />
    <InputDate id="shopping-list-from" @bind-Value="range.From" />
    <InputDate id="shopping-list-to" @bind-Value="range.To" />
    <FriggyButton Type="submit" Disabled="loading">
        @(loading ? "Calculando…" : "Calcular lista")
    </FriggyButton>
</EditForm>
```

La tabla debe conservar semántica accesible y admitir desplazamiento horizontal controlado en móvil. No añadir checkboxes, campos editables, persistencia del navegador ni acciones de compra.

Pruebas bUnit:

- valores iniciales y rango inverso;
- bloqueo contra doble envío;
- representación exacta de decimales y símbolos;
- línea cubierta y línea faltante;
- mismo ingrediente en dos unidades;
- vacío, error recuperable y recálculo;
- el componente no modifica el DTO recibido.

Añadir “Lista de la compra” a la navegación principal. No reutilizar el detalle del plan diario para el selector multifecha: son recorridos y estados diferentes.

## 14.7 — Rendimiento, E2E y visual

Medición reproducible mínima:

- 365 planes diarios;
- 3 comidas planificadas por día;
- 100 recetas con 8 ingredientes;
- 300 lotes;
- intervalo de 90 días.

Registrar número de consultas, filas materializadas, duración y SQL en `TestResults/performance/14/shopping-list.json`. La expectativa arquitectónica son dos consultas agrupadas y ninguna consulta por plan, receta, ingrediente o lote. El archivo es artefacto de ejecución y no se versiona salvo que la política vigente indique lo contrario.

Recorrido E2E:

1. Crear dos ingredientes, una receta y dos lotes en unidades exactas.
2. Crear dos planes diarios dentro del intervalo y uno fuera.
3. Configurar comensales diferentes, omitir una comida y completar otra.
4. Calcular la lista y comprobar requeridos, disponibles y compra.
5. Ajustar un lote, recalcular y comprobar el cambio sin refresco de entidad persistida.
6. Reiniciar servicios y obtener el mismo resultado a partir del estado almacenado.

Capturas revisadas en móvil y escritorio para lista vacía, comparación mixta y error de rango. Actualizar baselines solo tras revisar `actual` y `diff`.

## 14.8 — Secuencia TDD y gate

```powershell
dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.ShoppingLists.ShoppingListServiceTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Persistence.ShoppingListReadRepositoryTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Api.ShoppingListEndpointTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.ShoppingLists.ShoppingListPageTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-class "Friggy.EndToEndTests.ShoppingListJourneyTests"

./scripts/quality-gate.ps1
```

No se añade una suite Domain porque esta fase no introduce una entidad ni una nueva invariante de agregado. La validación del intervalo y la composición transversal pertenecen a Application.

## 14.9 — Documentación y cierre

Actualizar después de implementar:

- `docs/product/current-capabilities.md` y `roadmap.md`;
- guía de usuario de planificación/inventario y nueva guía de lista de compra;
- `docs/development/inventory-completion-walkthrough.md` o un recorrido específico;
- `docs/development/api.md`;
- README y navegación si enumeran capacidades.

Checklist final:

- [ ] La lista es calculada y no existe tabla ni escritura asociada.
- [ ] Intervalo inclusivo y sin límite funcional artificial.
- [ ] Solo cuentan entradas `Planned`.
- [ ] Comensales y líneas repetidas se agregan correctamente.
- [ ] Unidades distintas permanecen separadas.
- [ ] Stock válido hoy se descuenta una sola vez.
- [ ] SQL agrupado, cancelación y ausencia de N+1 verificados.
- [ ] API, bUnit, E2E, visual, documentación y gate están verdes.
