# Implementación ejecutable — Fase 2

- **Fase relacionada:** [Fase 2 — Catálogos](../phases/02-catalogs.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-data`, `dotnet-frontend`, `code-testing-planner`, `test-quality-auditor`, `dotnet-review`
- **Skills instaladas:** `architecture`, `modern-csharp`, `xunit`, `run-tests`, `assertion-quality`, `test-anti-patterns`, `entity-framework-core`, `minimal-apis`, `blazor`, `code-review`

La fase se ejecuta como cuatro verticales TDD: `Ingredient`, `UnitType`, `RecipeTag` y `MealType`. No se construyen los cuatro dominios completos antes de probar persistencia y UI del primero.

## Matriz de comportamiento

| Criterio | Primer rojo | Implementación mínima |
|---|---|---|
| Nombre obligatorio y normalizado | Domain | `CatalogName` y factory del agregado |
| Duplicado sin distinguir mayúsculas | Application e Integration | puerto `ExistsByNormalizedNameAsync` + índice único |
| CRUD y errores | Application y API | caso de uso + route group con `TypedResults` |
| Interfaz usable | Component | cliente HTTP tipado + componente Blazor |
| Borrado en uso | Integration | FK restrict + `409 ProblemDetails` |

## 2.1 — Vertical de Domain

Empezar por un test pequeño y específico:

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData("\t")]
public void Create_NameIsBlank_ThrowsDomainValidationException(string name)
{
    var exception = Assert.Throws<DomainValidationException>(
        () => Ingredient.Create(name));

    Assert.Equal("ingredient.name.required", exception.Code);
}
```

Implementación mínima orientativa:

```csharp
public sealed class Ingredient
{
    private Ingredient(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public CatalogName Name { get; private set; }

    public static Ingredient Create(string name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "ingredient.name.required"));

    public void Rename(string name) =>
        Name = CatalogName.Create(name, "ingredient.name.required");
}
```

El nombre visible se recorta; además se expone una representación normalizada estable para unicidad. No se filtran detalles de EF ni ASP.NET a Domain. Añadir luego pruebas de identidad, renombrado y normalización. `UnitType` añade símbolo obligatorio y `MealType` un orden no negativo.

Comando focalizado:

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.Ingredients.IngredientTests"
```

## 2.2 — Casos de uso de Application

Definir request/response como contratos inmutables. Crear puertos por raíz de agregado; evitar un repositorio genérico y no introducir MediatR/CQRS para este MVP.

```csharp
[Fact]
public async Task Execute_ValidName_PersistsAndReturnsNormalizedIngredient()
{
    var repository = new InMemoryIngredientRepository();
    var useCase = new CreateIngredient(repository);

    var result = await useCase.ExecuteAsync(
        new CreateIngredientRequest("  Tomate  "),
        CancellationToken.None);

    Assert.Equal("Tomate", result.Name);
    Assert.Contains(repository.Items, item =>
        item.Id == result.Id && item.Name.Value == "Tomate");
}
```

El fake es manual y nuevo por test. Las assertions verifican resultado y estado, no una secuencia interna de llamadas. Añadir rojos para duplicado normalizado, no encontrado, conflicto relacional y propagación de cancelación.

## 2.3 — PostgreSQL y repositorios

Mapear cada entidad en una clase `IEntityTypeConfiguration<T>`. Cada configuración, helper compartido de configuración e implementación de repositorio reside en su propio archivo dentro de `Configurations` o `Repositories`. Proyectar lecturas con `AsNoTracking`; no materializar entidades completas para DTO de listado.

```csharp
internal sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");
        builder.HasKey(x => x.Id);
        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(x => x.Value).HasColumnName("name").HasMaxLength(120);
            name.Property(x => x.Normalized).HasColumnName("normalized_name").HasMaxLength(120);
            name.HasIndex(x => x.Normalized).IsUnique();
        });
    }
}
```

La forma exacta del owned type y del índice se confirmará generando y revisando SQL de la migración. Un test de integración crea el registro, dispone el scope y lo recupera desde un segundo `DbContext`. Otro comprueba el índice real de PostgreSQL.

## 2.4 — Minimal APIs

Los handlers viven fuera de `Program.cs`, agrupados por recurso. API mapea excepciones de Application a `ProblemDetails` coherentes.

```csharp
public static class IngredientEndpoints
{
    public static RouteGroupBuilder MapIngredientEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ingredients").WithTags("Ingredients");
        group.MapPost("/", CreateAsync).WithName("CreateIngredient");
        group.MapGet("/", ListAsync).WithName("ListIngredients");
        group.MapGet("/{id:guid}", GetAsync).WithName("GetIngredient");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateIngredient");
        group.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteIngredient");
        return group;
    }

    private static async Task<Results<Created<IngredientResponse>, Conflict<ProblemDetails>>>
        CreateAsync(CreateIngredientRequest request, CreateIngredient useCase,
            CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(request, cancellationToken);
        return TypedResults.Created($"/api/ingredients/{result.Id}", result);
    }
}
```

El ejemplo muestra la forma, no el mecanismo final de conflictos: si se usa exception handling central, la firma debe reflejar solo resultados retornados directamente. Cada endpoint se prueba antes para éxito, body, `Location`, validación, `404`, `409` y persistencia posterior.

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task Post_DuplicateNormalizedName_ReturnsConflictProblemDetails()
{
    await fixture.Client.PostAsJsonAsync("/api/ingredients", new("Tomate"));

    var response = await fixture.Client.PostAsJsonAsync(
        "/api/ingredients", new CreateIngredientRequest(" tomate "));

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    Assert.NotNull(problem);
    Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
}
```

## 2.5 — Blazor y bUnit

Web usa `IIngredientsApiClient` y `HttpClient`; nunca Application services con acceso a repositorios. Cada cliente HTTP, interfaz y excepción pública reside en su propio archivo dentro de `Api`. El componente cubre carga, vacío, lista, validación, creación, edición, borrado y error HTTP.

```csharp
[Fact]
[Trait("Category", "Component")]
public void Submit_EmptyName_ShowsValidationAndDoesNotCallApi()
{
    using var context = new BunitContext();
    var api = new StubIngredientsApiClient();
    context.Services.AddSingleton<IIngredientsApiClient>(api);

    var component = context.Render<IngredientForm>();
    component.Find("form").Submit();

    Assert.Contains("obligatorio",
        component.Find(".validation-message").TextContent,
        StringComparison.OrdinalIgnoreCase);
    Assert.Empty(api.Created);
}
```

Preferir queries y texto accesible; usar `@key` al renderizar colecciones. No afirmar detalles CSS salvo que sean el comportamiento público.

## 2.6 — Repetir y cerrar

Repetir el corte Domain → Application → PostgreSQL → API → Blazor para UnitType, RecipeTag y MealType. Tras cada vertical:

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj
dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj
```

Auditar assertions y anti-patrones: cero `Assert.True(true)`, sleeps, estado compartido o tests que solo comprueban status. El handoff incluye contratos OpenAPI, migración revisada y los cuatro catálogos accesibles. Después habilitar [Fase 3](../phases/03-recipes.md).
