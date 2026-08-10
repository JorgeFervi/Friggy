# Guía operativa del repositorio

## Arquitectura

- Conserva la dirección `Domain <- Application <- Infrastructure <- Api`.
- `Friggy.Web` consume `Friggy.Api` por HTTP y no puede referenciar Infrastructure, `DbContext` ni repositorios.
- Domain no depende de frameworks. Application solo depende de Domain y de las abstracciones de DI necesarias para `AddApplication`.
- API es el composition root. No apliques migraciones durante una petición.

## Flujo TDD

- Todo comportamiento funcional o defecto comienza con un test xUnit v3 que falla por la razón esperada.
- Implementa el mínimo código para obtener verde y refactoriza manteniendo verdes las suites afectadas.
- Usa fakes manuales en Application, PostgreSQL/Testcontainers en integración, bUnit en componentes y Playwright en E2E.
- No uses EF Core InMemory, tests omitidos, dependencias de orden, estado mutable compartido ni esperas temporales fijas.

## Comandos

```powershell
./scripts/setup.ps1
./scripts/start.ps1
./scripts/test.ps1
./scripts/quality-gate.ps1
dotnet format Friggy.sln --verify-no-changes --no-restore
```

El repositorio usa xUnit v3 sobre Microsoft Testing Platform y .NET 10. Para focalizar pruebas utiliza `--filter-class`, `--filter-method` o `--filter-trait`, sin el separador `--`.

## Seguridad y configuración

- `.editorconfig`, `Directory.Build.props` y `Directory.Packages.props` son la fuente común de configuración.
- No versiones secretos, `.env`, artefactos de Playwright ni el contenido de `.friggy`.
- Los cambios de esquema requieren una migración explícita y una prueba de integración con PostgreSQL real.
