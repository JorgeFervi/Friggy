# Planificación de Friggy

Esta carpeta mantiene dos itinerarios consecutivos:

1. [Plan general del MVP](000-general-plan.md): fases 1 a 6, ya completadas.
2. [Plan general post-MVP](001-general-plan.md): correcciones, inventario, planificación avanzada, detalle de recetas y analítica.

Los documentos se organizan por estado:

- `completed/phases/` y `completed/implementation/`: planes y guías ya ejecutados.
- `phases/` e `implementation/`: trabajo futuro y sus subfases TDD.
- `discovery/`: decisiones funcionales que condicionan una fase.

## Fases completadas

| Fase | Plan | Implementación |
|---|---|---|
| 1 | [Arquitectura y banco de pruebas](completed/phases/01-architecture-and-test-harness.md) | [Guía](completed/implementation/01-architecture-and-test-harness-implementation.md) |
| 2 | [Catálogos](completed/phases/02-catalogs.md) | [Guía](completed/implementation/02-catalogs-implementation.md) |
| 3 | [Recetas](completed/phases/03-recipes.md) | [Guía](completed/implementation/03-recipes-implementation.md) |
| 4 | [Planificación semanal](completed/phases/04-weekly-planning.md) | [Guía](completed/implementation/04-weekly-planning-implementation.md) |
| 5 | [Integración completa](completed/phases/05-full-integration.md) | [Guía](completed/implementation/05-full-integration-implementation.md) |
| 6 | [Estabilización y piloto](completed/phases/06-stabilization-and-pilot.md) | [Guía](completed/implementation/06-stabilization-and-pilot-implementation.md) |

## Fases planificadas

| Fase | Plan | Implementación | Dependencia clave |
|---|---|---|---|
| 7 | [Correcciones del MVP y navegación](phases/07-mvp-corrections-and-navigation.md) | [Guía](implementation/07-mvp-corrections-and-navigation-implementation.md) | Fase 6 |
| 8 | [Inventario y finalización de comidas](phases/08-inventory-and-meal-completion.md) | [Guía](implementation/08-inventory-and-meal-completion-implementation.md) | Fase 7 y [decisiones](discovery/07-inventory-decisions.md) |
| 9 | [Planificación semanal avanzada](phases/09-advanced-weekly-planning.md) | [Guía](implementation/09-advanced-weekly-planning-implementation.md) | Fase 8 |
| 10 | [Ingredientes asociados a pasos](phases/10-recipe-step-ingredients.md) | [Guía](implementation/10-recipe-step-ingredients-implementation.md) | Fase 9 |
| 11 | [Panel principal y analítica](phases/11-dashboard-and-analytics.md) | [Guía](implementation/11-dashboard-and-analytics-implementation.md) | Fases 8–10 |

## Reglas de uso

- No comenzar una fase hasta que sus dependencias y precondiciones estén verdes.
- Cada comportamiento funcional o defecto comienza con un test xUnit v3 fallido por la razón esperada.
- Mantener cambios pequeños: rojo focalizado, implementación mínima, verde y refactorización.
- No incorporar estados rojos a la rama compartida.
- Actualizar estado, decisiones y handoff al terminar cada fase.
- Mover los documentos a `completed/` únicamente después del gate final.

## Estado actual

Las fases 1 a 6 están completadas y las decisiones funcionales de inventario han sido confirmadas. La siguiente unidad ejecutable es la Fase 7; la Fase 8 no debe comenzar hasta cerrar las correcciones y navegación con el quality gate en verde.
