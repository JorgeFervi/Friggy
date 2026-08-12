# Fase 9 — Planificación semanal avanzada

- **Estado:** Planificada y habilitada
- **Estimación:** 3–5 días
- **Dependencias:** [Fase 8 completada](../completed/phases/08-inventory-and-meal-completion.md)
- **Guía ejecutable:** [Implementación de la fase 9](../implementation/09-advanced-weekly-planning-implementation.md)

## Resultado esperado

Permitir que cada día use sus propios tipos de comida, asignar una hora prevista y registrar explícitamente una comida no realizada o sustituida sin falsear el inventario.

## Alcance

- Selección de los tipos de comida visibles para cada día del plan.
- Hora local opcional por comida planificada.
- Inicio de preparación calculado como hora prevista menos tiempo estimado de la receta.
- Estado explícito para comida omitida, con motivo y alternativa descriptiva.
- Conservación de los estados completados e irreversibles creados en la Fase 8.

La alternativa se registra inicialmente como texto y no genera consumo automático. Utilizar otra receta exige completar esa receta mediante el flujo normal.

## Criterios de salida

- Cada día muestra solo los tipos seleccionados y conserva al menos un hueco configurable.
- No se pierden asignaciones al reordenar o editar tipos del día.
- La hora de preparación se deriva de datos persistidos y no se guarda duplicada.
- Una comida omitida no consume inventario y conserva motivo y alternativa.
- Las transiciones desde planificada son deterministas y están cubiertas en Domain.
- Migración, API, bUnit, integración y E2E están en verde.

## Handoff

Proporcionar horarios y estados fiables a las fases de detalle de recetas y panel.
