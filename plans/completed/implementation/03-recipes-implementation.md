# Implementación ejecutable — Fase 3

- **Fase relacionada:** [Fase 3 — Recetas](../phases/03-recipes.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-data`, `dotnet-frontend`, `code-testing-planner`, `test-quality-auditor`, `dotnet-review`
- **Skills instaladas:** `architecture`, `modern-csharp`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`, `assertion-quality`, `test-anti-patterns`, `code-review`

> **Progreso:** fase completada. Las subfases 3.1 a 3.6 están verdes; la siguiente unidad ejecutable es la subfase 4.1.

`Recipe` es la raíz del agregado. `RecipeIngredient`, `RecipeStep` y los vínculos de etiquetas/tipos de comida se modifican únicamente mediante ella; no tendrán repositorios independientes.

## Orden e inventario

1. Invariantes de receta vacía.
2. Ingredientes y cantidades.
3. Pasos y orden.
4. Etiquetas y tipos de comida sin duplicados.
5. Caso de uso atómico de creación/actualización.
6. Mapeo EF, migración y repositorio.
7. API y `ProblemDetails`.
8. Formulario/detalle Blazor y bUnit.
9. Smoke Playwright de la vertical.

Los archivos se organizan primero por feature dentro de cada capa. En Application, `Recipes` se divide en `Dtos`, `Interfaces` y `Services`, con un tipo público por archivo y namespaces alineados, manteniendo la dirección de dependencias.

## 3.1 — Agregado en ciclos rojos pequeños

Primer ejemplo para cantidad:

```csharp
[Theory]
[InlineData("0")]
[InlineData("-0.01")]
public void AddIngredient_NonPositiveQuantity_ThrowsAndDoesNotMutate(string value)
{
    var recipe = Recipe.Create("Gazpacho", TimeSpan.FromMinutes(20));

    var exception = Assert.Throws<DomainValidationException>(() =>
        recipe.AddIngredient(
            Guid.NewGuid(),
            Guid.NewGuid(),
            decimal.Parse(value, CultureInfo.InvariantCulture),
            order: 1));

    Assert.Equal("recipe-ingredient.quantity.positive", exception.Code);
    Assert.Empty(recipe.Ingredients);
}
```

Implementación mínima orientativa:

```csharp
public void AddIngredient(
    Guid ingredientId,
    Guid unitTypeId,
    decimal quantity,
    int order)
{
    if (quantity <= 0)
        throw DomainValidationException.WithCode(
            "recipe-ingredient.quantity.positive");
    if (order < 0)
        throw DomainValidationException.WithCode(
            "recipe-ingredient.order.non-negative");
    if (ingredients.Any(item => item.Order == order))
        throw DomainConflictException.WithCode(
            "recipe-ingredient.order.duplicate");

    ingredients.Add(RecipeIngredient.Create(
        ingredientId, unitTypeId, quantity, order));
}
```

Añadir rojos separados para nombre, tiempo total, IDs, orden duplicado, paso vacío, tiempo del paso, tags duplicados, sustitución y eliminación. Exponer colecciones como solo lectura y preservar invariantes después de cualquier error.

## 3.2 — Application: operación atómica

Los DTO son records inmutables y no exponen entidades. El caso de uso valida la existencia de catálogos antes de guardar una sola vez la raíz completa.

```csharp
public sealed record CreateRecipeRequest(
    string Name,
    int EstimatedMinutes,
    IReadOnlyList<RecipeIngredientRequest> Ingredients,
    IReadOnlyList<RecipeStepRequest> Steps,
    IReadOnlySet<Guid> TagIds,
    IReadOnlySet<Guid> MealTypeIds);

public sealed class CreateRecipe
{
    public async Task<RecipeResponse> ExecuteAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        await references.EnsureExistAsync(request, cancellationToken);

        var recipe = Recipe.Create(
            request.Name,
            TimeSpan.FromMinutes(request.EstimatedMinutes));

        foreach (var item in request.Ingredients.OrderBy(x => x.Order))
            recipe.AddIngredient(item.IngredientId, item.UnitTypeId,
                item.Quantity, item.Order);

        await recipes.AddAsync(recipe, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RecipeMappings.ToResponse(recipe);
    }
}
```

El snippet abrevia pasos y tags: la implementación real debe incorporarlos antes del `SaveChangesAsync`. Probar referencias inexistentes, duplicado, cancelación y que un fallo no deje receta parcial. Los fakes verifican estado final, no llamadas privadas.

## 3.3 — EF Core y PostgreSQL

Usar configuraciones por entidad y tipos de unión explícitos para relaciones muchos-a-muchos cuando tengan identidad o restricciones relevantes.

```csharp
internal sealed class RecipeIngredientConfiguration
    : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.HasIndex(x => new { x.RecipeId, x.Order }).IsUnique();
        builder.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitType>()
            .WithMany()
            .HasForeignKey(x => x.UnitTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

Revisar la migración: precisión decimal, índices de orden, claves externas y delete behavior. Los tests usan PostgreSQL real y un segundo scope para leer. Probar transacción fallida, orden materializado y conflictos al borrar catálogos usados.

Para consultas de detalle, proyectar directamente a DTO con `AsNoTracking()` y orden explícito; evitar `Include` indiscriminado y problemas N+1.

## 3.4 — API

Mantener `/api/recipes` como route group con handlers fuera de `Program.cs`:

```csharp
group.MapPost("/", CreateAsync)
    .WithName("CreateRecipe")
    .Produces<RecipeResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status409Conflict);

private static async Task<Created<RecipeResponse>> CreateAsync(
    CreateRecipeRequest request,
    CreateRecipe useCase,
    CancellationToken cancellationToken)
{
    var recipe = await useCase.ExecuteAsync(request, cancellationToken);
    return TypedResults.Created($"/api/recipes/{recipe.Id}", recipe);
}
```

Escribir primero tests de `201 + Location + body`, `400`, `404` de referencias, `409`, actualización completa, borrado y recuperación. El exception handler central traduce códigos de negocio a RFC 9457/`ProblemDetails` consistente.

## 3.5 — Blazor y bUnit

Separar el estado editable del DTO de respuesta. Las filas dinámicas tienen claves estables; los componentes muestran carga, vacío, guardando, éxito y error.

```razor
@foreach (var ingredient in Model.Ingredients)
{
    <RecipeIngredientRow @key="ingredient.ClientId"
                         Model="ingredient"
                         OnRemove="RemoveIngredient" />
}
```

Ejemplo de prueba conductual:

```csharp
[Fact]
public void RemoveIngredient_TwoRows_RemovesOnlySelectedRowAndPreservesOrder()
{
    using var context = RecipeFormTestContext.Create();
    var component = context.Render<RecipeForm>(parameters => parameters
        .Add(p => p.InitialModel, RecipeFormModel.WithTwoIngredients()));

    component.FindAll("button[aria-label^='Eliminar ingrediente']")[0].Click();

    var rows = component.FindAll("[data-testid='ingredient-row']");
    Assert.Single(rows);
    Assert.Contains("Cebolla", rows[0].TextContent);
}
```

Cubrir validación de cantidad/tiempo, selección múltiple de etiquetas, añadir/mover/eliminar pasos, respuesta HTTP y navegación tras guardar. Fakes HTTP nuevos por test; usar `WaitForAssertion` si hay render asíncrono.

## 3.6 — Smoke y gate

Un E2E corto crea los catálogos mínimos y una receta; el recorrido completo se reserva para fase 5. Usar roles/labels y cero sleeps.

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.Recipes.RecipeTests"
dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj
dotnet test --solution Friggy.sln
```

El auditor comprueba assertions específicas, atomicidad, ausencia de N+1 evidente y cero hijos persistidos fuera del agregado. Entregar migración revisada, OpenAPI y receta operable por UI; después habilitar [Fase 4](../phases/04-weekly-planning.md).
