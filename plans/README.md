# Planificación de Friggy

Esta carpeta traduce la definición del MVP en un itinerario ejecutable. La documentación se divide en tres niveles:

1. [Plan general](000-general-plan.md): alcance, arquitectura, TDD, contratos, calendario y reglas comunes.
2. [Planes de fase](phases/): objetivo, entregables, dependencias y criterios de salida de cada fase.
3. [Guías de implementación](implementation/): subfases, ciclo rojo-verde-refactorización, ejemplos y comandos de verificación.

## Orden de ejecución

| Fase | Plan | Implementación | Agentes principales |
|---|---|---|---|
| 1 | [Arquitectura y banco de pruebas](phases/01-architecture-and-test-harness.md) | [Guía](implementation/01-architecture-and-test-harness-implementation.md) | `dotnet-router`, `dotnet-build`, `dotnet-review` |
| 2 | [Catálogos](phases/02-catalogs.md) | [Guía](implementation/02-catalogs-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 3 | [Recetas](phases/03-recipes.md) | [Guía](implementation/03-recipes-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 4 | [Planificación semanal](phases/04-weekly-planning.md) | [Guía](implementation/04-weekly-planning-implementation.md) | `dotnet-data`, `dotnet-review`, `dotnet-frontend` |
| 5 | [Integración completa](phases/05-full-integration.md) | [Guía](implementation/05-full-integration-implementation.md) | `dotnet-frontend`, `dotnet-build`, `dotnet-review` |
| 6 | [Estabilización y piloto](phases/06-stabilization-and-pilot.md) | [Guía](implementation/06-stabilization-and-pilot-implementation.md) | `dotnet-review`, `dotnet-build` |

## Reglas de uso

- No comenzar una fase hasta que sus dependencias estén en verde.
- Cada comportamiento de dominio, aplicación o API comienza con un test xUnit v3 fallido.
- No incorporar estados rojos a la rama compartida.
- Actualizar el estado y las decisiones del plan al terminar cada fase.
- Los ejemplos son patrones recomendados; los nombres finales deben conservar los contratos definidos en el plan general.

## Estado actual

Las fases 1 a 5 están completadas. La solución dispone de arquitectura y banco de pruebas, catálogos, recetas y planificación semanal operables de extremo a extremo mediante PostgreSQL, Minimal APIs, Blazor y Playwright, incluido el reinicio controlado de servicios sin pérdida de datos. La siguiente unidad ejecutable es la [Fase 6 — Estabilización y piloto](implementation/06-stabilization-and-pilot-implementation.md).
