# Fase 1 — Arquitectura y banco de pruebas

- **Estado:** En curso — subfases 1.1, 1.2 y 1.3 completadas
- **Estimación:** 2 días
- **Dependencias:** [Plan general](../000-general-plan.md)
- **Guía ejecutable:** [Implementación de la fase 1](../implementation/01-architecture-and-test-harness-implementation.md)

## Resultado esperado

Una solución restaurable y compilable con límites de Clean Architecture protegidos, PostgreSQL reproducible, xUnit v3 operativo y scripts locales idempotentes. No se implementará todavía comportamiento de negocio.

## Precondiciones

- .NET SDK 10, Docker Desktop y Docker Compose disponibles.
- Scaffold parcial auditado como no confiable hasta superar esta fase.
- Estrategia de configuración unificada y sin secretos versionados.

## Orden de ejecución

1. Auditar el scaffold, paquetes, referencias, `global.json` y warning `NU1903`.
2. Normalizar la solución, `Directory.Build.props`, `.editorconfig` y gestión de versiones.
3. Fijar y probar las referencias permitidas entre proyectos.
4. Configurar PostgreSQL en Docker Compose, health check y volumen persistente.
5. Configurar EF Core, design-time factory y una migración inicial vacía o de infraestructura.
6. Preparar los cinco proyectos de pruebas con xUnit v3, bUnit, Testcontainers y Playwright.
7. Implementar `setup.ps1`, `start.ps1` y `test.ps1` con smoke tests.
8. Ejecutar restauración, compilación, pruebas de arquitectura y arranque limpio.

## Entregables

- Cinco proyectos de producción y cinco proyectos de pruebas con responsabilidades explícitas.
- Reglas de dependencias automatizadas.
- Docker Compose de PostgreSQL y configuración de desarrollo.
- Scripts PowerShell idempotentes.
- Banco de pruebas capaz de mostrar rojo y verde de manera reproducible.
- ADR de Clean Architecture y ADR de estrategia TDD/testing.

## Criterios de salida

- `dotnet restore` y `dotnet build` terminan sin errores ni vulnerabilidades altas conocidas sin resolver.
- Domain no referencia otros proyectos; Application solo Domain; Infrastructure Application/Domain; API Application/Infrastructure; Web no referencia Infrastructure.
- PostgreSQL alcanza estado healthy y conserva datos tras reiniciar el contenedor.
- Una prueba xUnit v3 focalizada puede ejecutarse en cada proyecto de tests.
- Una prueba de integración puede crear PostgreSQL efímero mediante Testcontainers.
- Los scripts pueden ejecutarse dos veces sin destruir configuración o datos.

## Agentes y skills

- `dotnet-router`: clasificar el scaffold y delegar cada riesgo.
- `dotnet-build`: restore, build, paquetes, scripts y reproducibilidad.
- `dotnet-review`: límites arquitectónicos y vulnerabilidades.
- Skills: `project-setup`, `architecture`, `xunit`, `quality-ci`, `aspnet-core`.

## Handoff

Registrar versiones y comandos verificados en el README, marcar la fase en verde y habilitar [Fase 2 — Catálogos](02-catalogs.md).
