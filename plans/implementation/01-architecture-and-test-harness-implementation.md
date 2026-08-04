# Implementación ejecutable — Fase 1

- **Fase relacionada:** [Fase 1 — Arquitectura y banco de pruebas](../phases/01-architecture-and-test-harness.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-build`, `dotnet-review`, `code-testing-planner`, `test-quality-auditor`
- **Skills instaladas:** `project-setup`, `architecture`, `dotnet`, `modern-csharp`, `quality-ci`, `xunit`, `run-tests`, `assertion-quality`, `test-anti-patterns`, `aspnet-core`, `microsoft-extensions`

Esta guía consolida el scaffold existente; no vuelve a generarlo sin auditarlo. Al terminar debe existir una plataforma reproducible, pero ningún caso de uso funcional.

> **Progreso:** subfase 1.1 completada el 4 de agosto de 2026. SDK, MTP, Central Package Management, analizadores, formato, fuentes NuGet y smoke tests del scaffold están verificados. La siguiente unidad ejecutable es la subfase 1.2.

## Inventario de trabajo

| Subfase | Archivos principales | Evidencia |
|---|---|---|
| 1.1 Toolchain | `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig` | SDK y paquetes reproducibles |
| 1.2 Límites | los diez `.csproj`, `ArchitectureTests.cs` | dependencias permitidas |
| 1.3 Datos | `compose.yaml`, `FriggyDbContext`, factory de diseño, migración inicial | PostgreSQL healthy y migrable |
| 1.4 Harness | fixtures de Integration, Component y E2E | suites descubiertas sin placeholders |
| 1.5 Scripts | `scripts/setup.ps1`, `scripts/start.ps1`, `scripts/test.ps1` | segunda ejecución segura |
| 1.6 Gate | solución y ADR | restore, build, format y tests verdes |

## 1.1 — Fijar toolchain y resolver el scaffold

1. Fijar el SDK .NET 10 disponible, el runner Microsoft Testing Platform (MTP) y versiones centrales de paquetes.
2. Resolver `NU1903` actualizando o sustituyendo la dependencia que introduce `Microsoft.OpenApi 2.0.0`; no suprimir el aviso sin ADR.
3. Eliminar `Class1.cs` y reemplazar cada `Assert.True(true)` por una prueba real del harness.
4. No añadir `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` ni opciones VSTest: xUnit v3 se ejecutará sobre MTP.

Ejemplo adaptable de `global.json`:

```json
{
  "sdk": {
    "version": "10.0.302",
    "rollForward": "latestPatch"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Ejemplo de propiedades comunes, siguiendo `project-setup` y `quality-ci`:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
```

Antes de fijar `10.0.302`, comprobar `dotnet --version`; si el entorno usa otra revisión estable de .NET 10, registrar la decisión en el handoff.

## 1.2 — Proteger Clean Architecture con tests

La primera prueba roja debe demostrar una referencia prohibida del scaffold o fallar porque el guard todavía no existe. La implementación mínima inspeccionará referencias de ensamblado, sin introducir una biblioteca arquitectónica solo para este objetivo.

```csharp
public sealed class ArchitectureTests
{
    [Fact]
    [Trait("Category", "Architecture")]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var forbidden = new[]
        {
            "Friggy.Application",
            "Friggy.Infrastructure",
            "Friggy.Api",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore"
        };

        var references = typeof(Friggy.Domain.AssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null);

        Assert.DoesNotContain(references, forbidden.Contains);
    }
}
```

Crear un `AssemblyMarker` vacío por capa. Añadir tests equivalentes para Application, Infrastructure, API y Web. Web puede compartir contratos inmutables de Application, pero no Infrastructure, `FriggyDbContext` ni repositorios.

## 1.3 — Composition roots y PostgreSQL

Application e Infrastructure expondrán un único método de registro cada una. `Program.cs` se limita a componer y mapear endpoints; se declara parcial para `WebApplicationFactory`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.Run();

public partial class Program;
```

El Compose debe usar imagen con versión fijada, volumen nombrado y health check; no debe fijar `container_name` ni incluir secretos de producción.

```yaml
services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: friggy
      POSTGRES_USER: friggy
      POSTGRES_PASSWORD: friggy_local
    ports:
      - "5432:5432"
    volumes:
      - friggy-postgres:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U friggy -d friggy"]
      interval: 5s
      timeout: 3s
      retries: 12

volumes:
  friggy-postgres:
```

`FriggyDbContext` será `sealed`, se registrará como scoped y no se inyectará fuera de Infrastructure. Las migraciones se aplicarán de forma explícita por script, no en cada petición.

## 1.4 — Harness de pruebas

### Integración

Compartir el contenedor solo si resulta necesario para el rendimiento; cada clase tendrá una base física distinta y aplicará migraciones. El estado se limpia entre métodos. Nunca usar EF Core InMemory.

```csharp
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container =
        new PostgreSqlBuilder()
            .WithDatabase("friggy_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync() => await container.StartAsync();

    public async ValueTask DisposeAsync() => await container.DisposeAsync();
}
```

La fixture definitiva añadirá base aislada por clase, migración y reset por método. La factory de API sustituirá únicamente el registro del `DbContext`, preservando el composition root real.

### Componentes

Usar `BunitContext` de bUnit 2.x y fakes escritos a mano. Crear contexto y fake por test; ante render asíncrono usar `WaitForAssertion`, nunca `Task.Delay`.

### End-to-end

Exigir `FRIGGY_WEB_BASE_URL`, un `BrowserContext` por test y locators por rol o label. Desactivar paralelismo del proyecto E2E y capturar trace, screenshot, vídeo y logs solo al fallar.

## 1.5 — Scripts PowerShell idempotentes

Los tres scripts comienzan con modo estricto y cortan ante errores:

```powershell
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Checked {
    param([Parameter(Mandatory)][scriptblock]$Command)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "El comando terminó con código $LASTEXITCODE."
    }
}
```

- `setup.ps1`: valida herramientas, levanta PostgreSQL, restaura, aplica migraciones e instala Chromium.
- `start.ps1`: levanta PostgreSQL, espera `/health`, inicia API y Web y muestra sus URL.
- `test.ps1`: build único y suites Domain → Application → Integration → Component → E2E; termina en el primer fallo.
- Ningún script sobrescribe `.env`, elimina volúmenes ni presupone que Docker ya está activo.

## 1.6 — Secuencia de verificación

```powershell
dotnet restore Friggy.sln
dotnet build Friggy.sln --no-restore
dotnet format Friggy.sln --verify-no-changes --no-restore
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj
dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj
dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj
```

Para xUnit v3 sobre MTP usar `--filter-class`, `--filter-method` o `--filter-trait`; no usar `--filter "FullyQualifiedName~..."` ni el separador `--`.

## Auditoría y salida

- Cero tests tautológicos, omitidos, dependientes del orden o sin assertions.
- Cero referencias prohibidas y cero vulnerabilidades altas sin decisión explícita.
- PostgreSQL local y efímero funcionan desde una base vacía.
- `setup.ps1` y `start.ps1` superan dos ejecuciones consecutivas.
- Registrar versiones y comandos verificados; después habilitar [Fase 2](../phases/02-catalogs.md).
