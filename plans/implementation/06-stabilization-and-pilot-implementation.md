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

## 6.2 — Regresión guiada por TDD — Completada

La revisión priorizó corrección, integridad de datos, seguridad, errores de red, cancelación y lifecycle. Cada defecto confirmado se reprodujo primero con xUnit v3 sobre MTP y se corrigió con el cambio mínimo:

- **P1 — Resuelto:** una caída de la API durante alta, edición o borrado de catálogos escapaba del componente Blazor. Los cuatro catálogos conservan ahora el formulario y muestran el error recuperable.
- **P2 — Resuelto:** unidades, etiquetas y tipos de comida usaban `CancellationToken.None`. Ahora mantienen un token de lifecycle cancelado al liberar el componente.
- **P2 — Resuelto:** los conflictos de EF Core publicaban `DbUpdateException.Message`. `ProblemDetails` conserva el código `persistence.conflict`, pero devuelve un detalle estable sin información técnica.
- **P1 — Resuelto:** el recorrido E2E de persistencia conservaba un circuito Blazor del proceso Web detenido y podía competir con la reconexión automática. El navegador se desconecta de forma observable antes del reinicio y vuelve al plan después de los health checks.
- **Mejora posterior:** los listados que cargan agregados completos no se modifican sin una medición; se revisarán en 6.5.

La dependencia de `DateTime.Today` en `WeeklyPlanFormModel` se revisó expresamente. El cálculo lee el reloj una sola vez, conserva el lunes actual y avanza al siguiente desde domingo, por lo que no se demostró una regresión ni se añadió una abstracción temporal. Se elimina el ejemplo anterior porque usaba una propiedad `WeeklyPlanMealResponse.Recipe` inexistente y duplicaba el recorrido E2E que ya verifica la asignación después de reiniciar servicios.

La subfase añade nueve regresiones de componentes y una del manejador de excepciones. El gate final superó 222 pruebas: 59 Domain, 40 Application, 53 Integration, 55 Component y 15 End-to-End; 0 fallos y 0 omitidas. No cambia rutas, DTO, esquema ni migraciones. La siguiente unidad ejecutable es la subfase 6.3.

## 6.3 — Auditoría de calidad de tests — Completada

La auditoría se ejecutó el 10 de agosto de 2026 sobre las cinco suites xUnit v3/Microsoft Testing Platform. Se revisaron anti-patrones, diversidad de assertions, huecos conductuales y los puntos de integración API, componentes y E2E.

Resultado:

- **0 críticos y 0 altos:** no hay tests sin assertions, tautológicos, `async void`, esperas fijas, reloj real, excepciones tragadas, dependencia de orden ni tests omitidos. Los tests E2E usan `Expect` de Playwright y los tests de lifecycle delegan sus assertions en helpers verificables.
- **1 menor:** `Create_DescriptionIsMissing_NormalizesDescriptionToNull` comprueba únicamente `null`; es el resultado completo del contrato de normalización y no genera falsa confianza.
- **1 mejora de mantenibilidad:** 14 escenarios superan 30 líneas (4 superan 50) porque cubren recorridos completos de API, persistencia o navegador. Se conservan como escenarios de regresión coherentes y se podrán descomponer durante una futura revisión de mantenibilidad.
- Los 197 métodos de test (222 casos ejecutados por las teorías) cubren equality, boolean, null, exception, type, string, collection, negative, estado/efectos y assertions estructurales. No se detectaron huecos bloqueantes en status/body/headers/persistencia de API, DOM/efecto de componentes ni recuperación E2E tras reinicio.

El proveedor de cobertura no está referenciado todavía y no existe Cobertura XML. No se añade durante esta subfase: la configuración MTP y el diagnóstico CRAP pertenecen a 6.4, sin alterar el baseline de paquetes de 6.3.

Gate verificado: `scripts/test.ps1` superó 222/222 (59 Domain, 40 Application, 53 Integration, 55 Component y 15 End-to-End), 0 fallos y 0 omitidas; `dotnet format Friggy.sln --verify-no-changes --no-restore` limpio; auditoría NuGet sin vulnerabilidades. La siguiente unidad ejecutable es la subfase 6.4.

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
