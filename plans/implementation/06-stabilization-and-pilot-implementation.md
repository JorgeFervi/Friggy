# Implementación ejecutable — Fase 6

- **Fase relacionada:** [Fase 6 — Estabilización y piloto](../phases/06-stabilization-and-pilot.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-build`, `dotnet-review`, `test-quality-auditor`, `code-testing-planner`
- **Skills instaladas:** `quality-ci`, `code-review`, `run-tests`, `assertion-quality`, `test-anti-patterns`, `coverage-analysis`, `entity-framework-core`, `project-setup`

La fase estabiliza una aplicación funcional. No añade alcance por conveniencia: todo defecto se reproduce primero con un rojo y toda optimización requiere una medida.

## 6.1 — Ensayo de instalación limpia — Completada

En un entorno limpio: comprobar prerrequisitos, ejecutar `scripts/setup.ps1` dos veces, arrancar con PostgreSQL detenido, completar el recorrido, reiniciar servicios, verificar persistencia y ejecutar `scripts/test.ps1`.

Los scripts dan mensajes accionables y exit code distinto de cero ante requisitos ausentes. Nunca sobrescriben `.env` ni borran volúmenes del usuario.

Resultado verificado el 10 de agosto de 2026 desde una copia limpia de `HEAD` y un volumen PostgreSQL aislado:

- `scripts/setup.ps1` finalizó correctamente dos veces; la segunda ejecución no aplicó migraciones adicionales.
- `scripts/start.ps1` recuperó PostgreSQL detenido e inició API y Web con ambos endpoints disponibles.
- El recorrido ingrediente → receta → plan semanal persistió sus datos y la asignación después de reiniciar PostgreSQL, API y Web.
- `scripts/test.ps1` compiló en Release sin warnings y superó 212 pruebas: 59 Domain, 40 Application, 52 Integration, 46 Component y 15 End-to-End; 0 fallos y 0 omitidas.
- El entorno aislado se retiró y los servicios locales habituales se restauraron conservando su volumen original.

## 6.2 — Regresión guiada por TDD

```csharp
[Fact]
public async Task GetWeek_AfterApiRestart_ReturnsPreviouslyAssignedRecipe()
{
    var planId = await fixture.CreatePlannedRecipeAsync();
    await fixture.RestartApiAsync();

    var response = await fixture.Client.GetFromJsonAsync<WeeklyPlanResponse>(
        $"/api/weekly-plans/{planId}");

    Assert.NotNull(response);
    var assignedMeal = Assert.Single(
        response.Days.SelectMany(day => day.Meals)
            .Where(meal => meal.Recipe is not null));
    Assert.Equal("Gazpacho", assignedMeal.Recipe!.Name);
}
```

Después: implementación mínima, verde focalizado, refactor, suite de capa y solución completa. No desactivar tests inestables: aislar reloj, datos, red o lifecycle.

## 6.3 — Auditoría de calidad de tests

Ejecutar en orden: `test-anti-patterns`, `assertion-quality`, huecos conductuales, `coverage-analysis` y revisión profunda solo donde haya hallazgos.

Gate: cero tests sin assertions/tautológicos, `async void`, sleeps, reloj real, dependencia de orden u omitidos. API comprueba status, body/headers y persistencia; componentes DOM y efecto; E2E datos recuperados tras reinicio.

## 6.4 — Cobertura como diagnóstico

Configurar un proveedor compatible con MTP, preferiblemente `Microsoft.Testing.Extensions.CodeCoverage`, y generar Cobertura por proyecto. La opción exacta se verifica contra la versión instalada antes de incorporarla a scripts.

```powershell
./.codex/skills/coverage-analysis/scripts/Extract-MethodCoverage.ps1 `
  -CoverageFile ./TestResults/coverage.cobertura.xml

./.codex/skills/coverage-analysis/scripts/Compute-CrapScores.ps1 `
  -CoverageFile ./TestResults/coverage.cobertura.xml
```

No imponer un porcentaje arbitrario ni escribir tests de getters triviales. Añadir pruebas donde exista regla, rama, integración o fallo sin protección.

## 6.5 — Rendimiento medido

`optimizing-ef-core-queries` se recomendó automáticamente, pero se reserva para esta subfase y solo se instalará/empleará si una medida revela un problema. Primero registrar duración, filas y SQL.

```csharp
return await dbContext.Recipes
    .AsNoTracking()
    .OrderBy(recipe => recipe.Name.Normalized)
    .Select(recipe => new RecipeListItemResponse(
        recipe.Id,
        recipe.Name.Value,
        recipe.EstimatedTime.TotalMinutes))
    .ToListAsync(cancellationToken);
```

Evitar `Include` masivo, tracking en lecturas y N+1. No añadir caché o infraestructura sin evidencia y alcance acordado.

## 6.6 — Gate automatizado

```powershell
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

dotnet build Friggy.sln --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$projects = @(
    'tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj',
    'tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj',
    'tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj',
    'tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj',
    'tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj'
)

foreach ($project in $projects) {
    dotnet test --project $project --no-build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
```

Verificar `--no-build` contra la versión MTP instalada; si no está soportado, usar la opción equivalente documentada.

## 6.7 — Piloto y cierre

Registrar tiempo de instalación, tareas sin ayuda, errores y artefactos, lenguaje confuso, severidad de defectos y mejoras fuera de alcance. Corregir defectos con rojo previo. Actualizar README, ADR y documentación MVP para reflejar Web → HTTP → API, PostgreSQL/Docker y scripts.

```powershell
dotnet restore Friggy.sln
dotnet build Friggy.sln --no-restore
dotnet format Friggy.sln --verify-no-changes --no-restore
./scripts/test.ps1
```

La entrega termina cuando una instalación limpia, todas las suites y el piloto acordado están verdes; no al alcanzar un número de cobertura o una fecha sin gates.
