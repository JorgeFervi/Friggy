# Contribuir a Friggy

Gracias por ayudar a mejorar Friggy. Este repositorio sigue Clean Architecture, TDD con xUnit v3 y una puerta de calidad común. Antes de cambiar código, prepara el entorno con la [guía de desarrollo](docs/development/environment.md) y revisa la [arquitectura](docs/development/architecture.md).

## Flujo de trabajo

Todo comportamiento funcional o defecto comienza con un test que falle por la razón esperada:

1. Escribe el test xUnit v3 en la suite más cercana al comportamiento.
2. Ejecuta el filtro mínimo y confirma el rojo esperado.
3. Implementa el menor cambio que permita obtener verde.
4. Refactoriza manteniendo verdes las suites afectadas.
5. Ejecuta `./scripts/quality-gate.ps1` antes de dar el cambio por terminado.

No se aceptan tests omitidos, dependencias de orden, estado mutable compartido, esperas temporales fijas ni EF Core InMemory. Application utiliza fakes manuales; la integración de datos utiliza PostgreSQL real mediante Testcontainers.

## Dónde realizar un cambio

| Tipo de cambio | Ubicación principal | Prueba mínima |
|---|---|---|
| Invariante o transición de negocio | `Friggy.Domain` | Domain |
| Caso de uso, conflicto o referencia | `Friggy.Application` | Application con fake manual |
| Consulta, repositorio, transacción o esquema | `Friggy.Infrastructure` | Integration con PostgreSQL |
| Ruta, contrato o semántica HTTP | `Friggy.Api` y DTO de Application | Integration |
| Interacción o presentación Blazor | `Friggy.Web` | Component con bUnit |
| Recorrido crítico visible | Web → HTTP → API → PostgreSQL | E2E con Playwright |

`Friggy.Web` puede compartir contratos inmutables de Application, pero consume la funcionalidad exclusivamente por HTTP. No debe referenciar Infrastructure, `DbContext`, repositorios ni entidades persistentes.

## Comandos principales

```powershell
./scripts/setup.ps1
./scripts/start.ps1
./scripts/test.ps1
./scripts/quality-gate.ps1
./scripts/stop.ps1
```

Los filtros de Microsoft Testing Platform se pasan directamente, sin el separador `--`. Ejemplos y comandos por suite están en [Estrategia y ejecución de pruebas](docs/development/testing.md).

## Base de datos y cambios visuales

Todo cambio de esquema requiere una migración explícita de Entity Framework Core y una prueba de integración con PostgreSQL. Las migraciones se aplican durante la preparación del entorno, nunca dentro de una petición. Consulta [Persistencia y migraciones](docs/development/database.md).

Los cambios visuales deben respetar los tokens, componentes y viewports aprobados. Revisa siempre las capturas `actual` y `diff` antes de ejecutar:

```powershell
./scripts/update-visual-baselines.ps1 -Accept
```

El procedimiento completo está en [Interfaz, accesibilidad y regresión visual](docs/development/ui-guidelines.md).

## Documentación y seguridad

Actualiza la documentación cuando cambien funcionalidades visibles, endpoints, configuración, scripts, migraciones o límites arquitectónicos. No versiones secretos, archivos `.env`, `.friggy`, `TestResults/visual` ni artefactos de Playwright.

Antes de entregar un cambio, comprueba:

* El test rojo inicial demostraba el comportamiento o defecto.
* Las dependencias continúan apuntando hacia Domain.
* Web sigue usando HTTP para acceder a la aplicación.
* Los cambios de esquema incluyen migración e integración real.
* Las capturas visuales modificadas fueron revisadas y explicadas.
* README, guías y ADR se actualizaron cuando correspondía.
* `./scripts/quality-gate.ps1` termina correctamente.
