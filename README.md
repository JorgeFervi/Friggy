# Friggy

Friggy es una aplicación para organizar y planificar tus comidas semanales de forma sencilla.

La aplicación te permite registrar los alimentos que tienes en casa, guardar tus propias recetas, definir distintos tipos de comida y organizar tu menú mediante un calendario semanal.

Además, Friggy puede conectar tus datos con un sistema de inteligencia artificial para ayudarte a:

* Crear una planificación semanal adaptada a tus preferencias.
* Aprovechar los alimentos que ya tienes disponibles.
* Analizar tus hábitos alimentarios y ofrecerte recomendaciones de mejora.
* Descubrir nuevos platos y recetas basados en tus gustos.

El objetivo de Friggy es facilitar la planificación de las comidas, reducir el desperdicio de alimentos y ayudarte a mantener una alimentación más variada y organizada.

## Tecnologías previstas para el MVP

* **Backend:** ASP.NET Core con Minimal APIs sobre .NET 10.
* **Interfaz:** Blazor Web App con interactividad en servidor.
* **Persistencia:** PostgreSQL ejecutado localmente mediante Docker Compose.
* **Acceso a datos:** Entity Framework Core con migraciones versionadas.

La definición funcional, el alcance y el calendario de desarrollo se encuentran en [Definición del MVP](docs/use-cases/001-mvp.md). Las decisiones arquitectónicas se documentan en [docs/adr](docs/adr).

Las decisiones que gobiernan la implementación están recogidas en [Clean Architecture](docs/adr/003-clean-architecture.md) y [TDD y estrategia de pruebas](docs/adr/004-tdd-and-testing-strategy.md).

## Flujo del MVP

La interfaz `Friggy.Web` se ejecuta con Blazor Server y consume `Friggy.Api` exclusivamente por HTTP. La API aplica los casos de uso de `Friggy.Application` y persiste mediante `Friggy.Infrastructure` en PostgreSQL ejecutado por Docker Compose:

```text
Friggy.Web ──HTTP──> Friggy.Api ──> Application ──> Infrastructure ──> PostgreSQL/Docker
```

El piloto técnico local está registrado en [plans/pilot/20260810-local-technical-pilot.md](plans/pilot/20260810-local-technical-pilot.md).

## Entorno verificado

| Componente | Versión |
|---|---|
| .NET SDK | 10.0.302 |
| PostgreSQL | 17.6-alpine |
| Entity Framework Core | 10.0.10 |
| Npgsql para EF Core | 10.0.3 |
| xUnit v3 / MTP v2 | 3.2.2 |
| Testcontainers.PostgreSql | 4.13.0 |
| bUnit | 2.9.0 |
| Playwright para .NET | 1.61.0 |

La puerta de calidad de la fase 1 se verificó con Docker Engine 29.6.2 y Docker Compose 5.3.1. Son versiones del entorno validado, no credenciales ni requisitos de producción.

## Ejecución local

Desde PowerShell, ejecutar los scripts en este orden:

```powershell
./scripts/setup.ps1
./scripts/start.ps1
```

`setup.ps1` comprueba .NET y Docker, arranca PostgreSQL, restaura herramientas y paquetes, aplica las migraciones e instala Chromium para Playwright. Puede ejecutarse de nuevo sin eliminar datos ni recrear la configuración local.

`start.ps1` inicia la API en `http://localhost:5292` y la aplicación Web en `http://localhost:5179`. Si ya están disponibles, no crea procesos duplicados. Los logs locales se guardan bajo `.friggy/logs`, que no se versiona.

Para compilar una vez y ejecutar todas las suites en orden:

```powershell
./scripts/test.ps1
```

El script usa la configuración `Release` para no interferir con los procesos locales `Debug` iniciados por `start.ps1`, y termina inmediatamente si falla cualquier comando o suite. El parámetro `-SkipBuild` permite reutilizar una compilación previa desde el gate completo.

## Puerta de calidad

Antes de incorporar cambios, ejecutar:

```powershell
./scripts/quality-gate.ps1
```

El gate restaura con `NuGet.Config`, compila Release, verifica formato, ejecuta las cinco suites mediante Microsoft Testing Platform y falla si la auditoría JSON de NuGet encuentra vulnerabilidades.

Para ejecutar únicamente las reglas arquitectónicas con xUnit v3 sobre MTP:

```powershell
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-trait "Category=Architecture"
```

## Catálogos del MVP

La aplicación permite administrar ingredientes, unidades, etiquetas de receta y tipos de comida desde Blazor y mediante las rutas `/api/ingredients`, `/api/unit-types`, `/api/recipe-tags` y `/api/meal-types`.

La migración `AddCatalogs` incorpora unidades y tipos de comida iniciales con identificadores estables. Los identificadores públicos se encuentran en `Friggy.Domain.Catalogs.CatalogSeedIds`; incluyen gramo, kilogramo, mililitro, litro, unidad, cucharadita, cucharada, desayuno, comida y cena.
