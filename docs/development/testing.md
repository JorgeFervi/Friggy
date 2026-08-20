# Estrategia y ejecución de pruebas

> **Estado:** vigente · **Decisión principal:** [ADR 004](../adr/004-tdd-and-testing-strategy.md)

Friggy utiliza xUnit v3 sobre Microsoft Testing Platform. Todo comportamiento funcional o defecto comienza con rojo, continúa con la implementación mínima y termina con refactorización manteniendo verdes las suites afectadas.

## Suites

| Suite | Responsabilidad | Dependencias reales |
|---|---|---|
| Domain | Invariantes y transiciones | Ninguna infraestructura |
| Application | Casos de uso | Fakes manuales |
| Integration | API, repositorios, migraciones y arquitectura | PostgreSQL/Testcontainers |
| Component | Páginas y componentes Blazor | bUnit y dobles HTTP manuales |
| E2E | Recorridos Web → API → PostgreSQL | Chromium, procesos reales y PostgreSQL/Testcontainers |

No uses EF Core InMemory, tests omitidos, estado mutable compartido, dependencias de orden ni esperas temporales fijas.

## Ejecutar todas las suites

```powershell
./scripts/test.ps1
```

El script compila Release y ejecuta las cinco suites en orden. Si ya existe una compilación Release válida:

```powershell
./scripts/test.ps1 -SkipBuild
```

Los E2E preparan un PostgreSQL aislado, aplican migraciones e inician API y Web en puertos libres. Los procesos locales Debug iniciados por `start.ps1` no se reutilizan como entorno de la colección full-stack.

## Ejecutar una selección

MTP en .NET 10 recibe los filtros directamente, sin un separador `--`:

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj --configuration Release --filter-class "Friggy.Domain.Tests.Recipes.RecipeTests"
```

```powershell
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj --configuration Release --filter-method "Friggy.IntegrationTests.Api.HealthEndpointTests.GetHealth_ComposedApplication_ReturnsHealthy"
```

```powershell
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj --configuration Release --filter-trait "Category=Architecture"
```

Usa `--filter-class`, `--filter-method` o `--filter-trait` y conserva el orden `dotnet test --project <proyecto> [opciones]`.

## Regresión visual

Los E2E visuales comparan capturas deterministas con baselines versionados bajo `tests/Friggy.EndToEndTests/VisualBaselines`. Las capturas actuales y diferencias quedan en `TestResults/visual`.

Ante un cambio visual intencionado:

1. Ejecuta el recorrido afectado y deja que falle la comparación.
2. Revisa manualmente las capturas `actual` y `diff`.
3. Explica por qué cambia la interfaz.
4. Acepta únicamente los baselines revisados:

```powershell
./scripts/update-visual-baselines.ps1 -Accept
```

Ni `setup.ps1` ni el gate aceptan imágenes automáticamente.

## Puerta de calidad

```powershell
./scripts/quality-gate.ps1
```

El gate restaura con los archivos bloqueados, compila Release sin warnings, verifica `dotnet format`, ejecuta las suites y audita vulnerabilidades de NuGet y pnpm. Es el criterio final de aceptación del repositorio.
