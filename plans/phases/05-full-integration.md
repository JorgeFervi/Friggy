# Fase 5 — Integración completa

- **Estado:** Bloqueada por fase 4
- **Estimación:** 2 días
- **Dependencias:** [Fase 4](04-weekly-planning.md)
- **Guía ejecutable:** [Implementación de la fase 5](../implementation/05-full-integration-implementation.md)

## Resultado esperado

Web, API y PostgreSQL funcionan como procesos separados y el recorrido principal está automatizado con Playwright.

## Precondiciones

- Contratos de catálogos, recetas y planificación congelados.
- Suites por capa en verde.

## Orden de ejecución

1. Endurecer clientes HTTP tipados, timeouts, cancelación y deserialización de `ProblemDetails`.
2. Completar tests bUnit para estados de carga, vacío, éxito y error.
3. Crear fixture end-to-end que arranque PostgreSQL, API y Web y espere health checks.
4. Escribir primero el escenario Playwright del recorrido principal y observar el fallo.
5. Completar únicamente el wiring o UI que impida poner el escenario en verde.
6. Añadir escenario de validación y persistencia después de reinicio.
7. Integrar los recorridos en `scripts/test.ps1` y recopilar trazas al fallar.

## Entregables

- Clientes HTTP de Web sin acceso directo a backend interno.
- Component tests representativos con bUnit.
- Fixtures de proceso y datos aislados para Playwright.
- Escenario principal automatizado en Chromium.
- Evidencias de error con trace/screenshot solo al fallar.

## Criterios de salida

- `start.ps1` inicia base, API y Web y publica sus URL.
- Playwright completa catálogo → receta → semana → asignación.
- Reiniciar servicios no pierde datos del entorno local.
- Los tests son repetibles, no dependen de orden ni datos previos y limpian sus recursos efímeros.
- La UI presenta mensajes accionables ante API no disponible o validación.

## Agentes y skills

- `dotnet-frontend`: Blazor, HTML semántico y experiencia de error.
- `dotnet-build`: orquestación, procesos y test runner.
- `dotnet-review`: aislamiento, flakiness y contratos.
- Skills: `blazor`, `xunit`, `run-tests`, `playwright-visual-testing`, `aspnet-core`.

## Handoff

Conservar una ejecución verde completa y habilitar [Fase 6 — Estabilización y piloto](06-stabilization-and-pilot.md).
