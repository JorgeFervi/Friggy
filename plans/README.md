# Planificación de Friggy

Esta carpeta mantiene cuatro itinerarios consecutivos:

1. [Plan general del MVP](000-general-plan.md): fases 1 a 6, ya completadas.
2. [Plan general post-MVP](001-general-plan.md): correcciones, inventario, planificación avanzada, detalle de recetas y evolución visual.
3. Fases 12 a 15: sustitución por planes diarios, plantillas, lista de la compra y consolidación de UI/UX.
4. [Fase 16](../docs/plans/phases/16-unit-conversions.md): conversiones y usos contextuales de unidades.

Los documentos se organizan por estado:

- `completed/phases/` y `completed/implementation/`: planes y guías ya ejecutados.
- `phases/` e `implementation/`: implementaciones presentes en código cuyo cierre formal todavía no está documentado.
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
| 7 | [Correcciones del MVP y navegación](completed/phases/07-mvp-corrections-and-navigation.md) | [Guía](completed/implementation/07-mvp-corrections-and-navigation-implementation.md) |
| 8 | [Inventario y finalización de comidas](completed/phases/08-inventory-and-meal-completion.md) | [Guía](completed/implementation/08-inventory-and-meal-completion-implementation.md) |
| 9 | [Planificación semanal avanzada](completed/phases/09-advanced-weekly-planning.md) | [Guía](completed/implementation/09-advanced-weekly-planning-implementation.md) |
| 10 | [Ingredientes asociados a pasos](completed/phases/10-recipe-step-ingredients.md) | [Guía](completed/implementation/10-recipe-step-ingredients-implementation.md) |
| 11 | [Sistema visual y experiencia responsive](completed/phases/11-visual-system-and-responsive-ui.md) | [Guía](completed/implementation/11-visual-system-and-responsive-ui-implementation.md) |
| 12 | [Planificación diaria](completed/phases/12-daily-planning.md) | [Guía](completed/implementation/12-daily-planning-implementation.md) |

## Implementadas en código con cierre formal pendiente

| Fase | Plan | Implementación | Estado verificable |
|---|---|---|---|
| 13 | [Plantillas de planes diarios](phases/13-daily-plan-templates.md) | [Guía](implementation/13-daily-plan-templates-implementation.md) | Domain, Application, PostgreSQL, API y Web presentes; faltan E2E/visual y gate final |
| 14 | [Lista de la compra calculada](phases/14-shopping-list.md) | [Guía](implementation/14-shopping-list-implementation.md) | Implementación y recorridos E2E/visuales presentes; gate final no registrado |
| 15 | [Consolidación de UI y UX](phases/15-ui-ux-consolidation.md) | [Guía](implementation/15-ui-ux-consolidation-implementation.md) | Cambios de interfaz presentes; checklist y gate final no registrados |
| 16 | [Conversiones y usos contextuales](../docs/plans/phases/16-unit-conversions.md) | [Guía](../docs/plans/implementation/16-unit-conversions-implementation.md) | Implementación presente; PostgreSQL, E2E, visual y gate pendientes en un entorno con Docker |

## Reglas de uso

- No comenzar una fase hasta que sus dependencias y precondiciones estén verdes.
- Cada comportamiento funcional o defecto comienza con un test xUnit v3 fallido por la razón esperada.
- Mantener cambios pequeños: rojo focalizado, implementación mínima, verde y refactorización.
- No incorporar estados rojos a la rama compartida.
- Actualizar estado, decisiones y handoff al terminar cada fase.
- Mover los documentos a `completed/` únicamente después del gate final.

## Estado actual

Las fases 1 a 12 están completadas y conservan evidencia de cierre. Las fases 13 a 16 están implementadas en el código actual, pero permanecen fuera de `completed/` hasta registrar o completar sus validaciones pendientes. “Implementada” no equivale a “cerrada”: solo se moverá cada pareja de documentos después de un gate completo y de actualizar su checklist con evidencia real.
