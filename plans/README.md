# Planificación de Friggy

Esta carpeta traduce la definición del MVP en un itinerario ejecutable. La documentación se divide en tres niveles:

1. [Plan general](000-general-plan.md): alcance, arquitectura, TDD, contratos, calendario y reglas comunes.
2. [Planes de fase completados](completed/phases/): objetivo, entregables, dependencias y criterios de salida de cada fase completada.
3. [Guías de implementación completadas](completed/implementation/): subfases, ciclo rojo-verde-refactorización, ejemplos y comandos de verificación.

## Orden de ejecución

| Fase | Plan | Implementación | Agentes principales |
|---|---|---|---|
| 1 | [Arquitectura y banco de pruebas](completed/phases/01-architecture-and-test-harness.md) | [Guía](completed/implementation/01-architecture-and-test-harness-implementation.md) | `dotnet-router`, `dotnet-build`, `dotnet-review` |
| 2 | [Catálogos](completed/phases/02-catalogs.md) | [Guía](completed/implementation/02-catalogs-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 3 | [Recetas](completed/phases/03-recipes.md) | [Guía](completed/implementation/03-recipes-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 4 | [Planificación semanal](completed/phases/04-weekly-planning.md) | [Guía](completed/implementation/04-weekly-planning-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 5 | [Integración completa](completed/phases/05-full-integration.md) | [Guía](completed/implementation/05-full-integration-implementation.md) | `dotnet-frontend`, `dotnet-build`, `dotnet-review` |
| 6 | [Estabilización y piloto](completed/phases/06-stabilization-and-pilot.md) | [Guía](completed/implementation/06-stabilization-and-pilot-implementation.md) | `dotnet-review`, `dotnet-build` |

## Reglas de uso

- No comenzar una fase hasta que sus dependencias estén en verde.
- Cada comportamiento de dominio, aplicación o API comienza con un test xUnit v3 fallido.
- No incorporar estados rojos a la rama compartida.
- Actualizar el estado y las decisiones del plan al terminar cada fase.
- Los ejemplos son patrones recomendados; los nombres finales deben conservar los contratos definidos en el plan general.

## Estado actual

Las fases 1 a 6 están completadas. La subfase 6.7 cerró el piloto técnico local con setup idempotente, API/Web disponibles, 15/15 E2E y sin defectos bloqueantes confirmados. El MVP queda como candidato a piloto guiado; el registro está en [plans/pilot/20260810-local-technical-pilot.md](pilot/20260810-local-technical-pilot.md).

Antes de abrir una nueva fase deben completarse el [piloto humano guiado](pilot/guided-human-pilot.md) y el [registro de decisiones de inventario](discovery/07-inventory-decisions.md). El frigorífico virtual se planificará después como una capacidad dentro de las capas actuales; la lista de la compra y la IA permanecen fuera de esa primera entrega.
