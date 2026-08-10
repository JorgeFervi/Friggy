# Implementación ejecutable — Fase 5

- **Fase relacionada:** [Fase 5 — Integración completa](../phases/05-full-integration.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-frontend`, `dotnet-build`, `code-testing-planner`, `test-quality-auditor`, `dotnet-review`
- **Skills instaladas:** `aspnet-core`, `microsoft-extensions`, `blazor`, `xunit`, `run-tests`, `assertion-quality`, `test-anti-patterns`, `playwright-visual-testing`, `quality-ci`

Esta fase une verticales ya verdes. No añade reglas de negocio nuevas salvo que primero se expresen mediante un test rojo en su capa propietaria.

## 5.1 — Contrato Web–API — Completada

Registrar clientes tipados mediante options validadas al inicio. Web no referencia Infrastructure ni `DbContext`.

```csharp
builder.Services
    .AddOptions<FriggyApiOptions>()
    .BindConfiguration(FriggyApiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IRecipesApiClient, RecipesApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<FriggyApiOptions>>().Value;
    client.BaseAddress = options.BaseUrl;
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

Cada cliente propaga `CancellationToken`, usa serialización web estándar y traduce `400/404/409` a un resultado comprensible conservando `ProblemDetails`.

```csharp
public async Task<RecipeResponse> CreateAsync(
    CreateRecipeRequest request,
    CancellationToken cancellationToken)
{
    using var response = await httpClient.PostAsJsonAsync(
        "api/recipes", request, cancellationToken);

    if (!response.IsSuccessStatusCode)
        throw await ApiProblemException.FromResponseAsync(response, cancellationToken);

    return await response.Content.ReadFromJsonAsync<RecipeResponse>(
        cancellationToken: cancellationToken)
        ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
}
```

## 5.2 — bUnit de integración de componentes — Completada

```csharp
[Fact]
[Trait("Category", "Component")]
public void Save_ApiReturnsConflict_ShowsMessageAndKeepsFormData()
{
    using var context = new BunitContext();
    var api = RecipesApiStub.Conflict("Ya existe una receta con ese nombre.");
    context.Services.AddSingleton<IRecipesApiClient>(api);

    var component = context.Render<RecipeForm>();
    component.Find("input[aria-label='Nombre']").Change("Gazpacho");
    component.Find("form").Submit();

    component.WaitForAssertion(() =>
        Assert.Contains("Ya existe", component.Find("[role='alert']").TextContent));
    Assert.Equal("Gazpacho",
        component.Find("input[aria-label='Nombre']").GetAttribute("value"));
}
```

Usar `BunitContext` y fakes nuevos por test. Cubrir navegación, loading, vacío y errores HTTP; nunca `Task.Delay` para esperar render.

## 5.3 — Fixture full-stack — Completada

El entorno E2E es exclusivo: PostgreSQL efímero, migraciones antes de API, API y Web con health checks, `FRIGGY_WEB_BASE_URL` para el runner y reinicio controlado sin borrar el volumen. No reutiliza datos locales. Espera señales observables y conserva stdout/stderr.

## 5.4 — Recorrido Playwright .NET — Completada

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

public sealed class MainJourneyTests : PageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task User_CreatesAndRecoversAPlannedRecipeAfterRestart()
    {
        var baseUrl = Environment.GetEnvironmentVariable("FRIGGY_WEB_BASE_URL")
            ?? throw new InvalidOperationException(
                "FRIGGY_WEB_BASE_URL is required for E2E tests.");

        await Page.GotoAsync(baseUrl);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Ingredientes" }).ClickAsync();
        await Page.GetByLabel("Nombre").FillAsync("Tomate");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "Tomate" }))
            .ToBeVisibleAsync();

        // Sustituir por pasos reales: demás catálogos, receta, semana,
        // asignación, reinicio mediante fixture y recuperación.
    }
}
```

El comentario se reemplaza durante ciclos pequeños. No se admite `Thread.Sleep`, `Task.Delay` ni CSS frágil. Fechas y nombres son únicos y deterministas.

## 5.5 — Determinismo y artefactos — Completada

`playwright-visual-testing` está diseñada para Playwright Test de Node (`toHaveScreenshot`), no para `Microsoft.Playwright.Xunit.v3`. Aquí solo se aplican sus principios: fijar locale/zona horaria/viewport/color scheme/escala, desactivar animaciones, guardar trace/screenshot/vídeo/logs al fallar y no aceptar baselines automáticamente.

No se añade Node ni una suite visual paralela. Si se aprueba más adelante, será una superficie separada con baselines revisadas y versionadas.

## 5.6 — Gate — Completada

Escenarios mínimos: API inaccesible, errores `400/404/409` visibles, sustitución/retirada, persistencia tras reinicio y estado vacío limpio.

```powershell
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj
dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj
dotnet test --solution Friggy.sln
```

El recorrido principal verifica datos recuperados, no solo que una página carga. Después habilitar [Fase 6](../phases/06-stabilization-and-pilot.md).

El gate cubre el estado vacío con bUnit, la traducción y presentación de errores `400/404/409`, la sustitución y retirada de recetas, la indisponibilidad real de la API y la recuperación de la asignación semanal después de reiniciar API y Web sin reemplazar PostgreSQL.
