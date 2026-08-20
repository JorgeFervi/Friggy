# Recorrido técnico — Planificación semanal

> **Estado:** vigente

## Objetivo

Este recorrido muestra cómo una interacción del calendario atraviesa Web, API, Application, Domain e Infrastructure sin romper sus límites.

```text
WeeklyPlanCalendar -> WeeklyPlanDetails -> WeeklyPlanApiClient
    -> WeeklyPlanEndpoints -> WeeklyPlanService
    -> WeeklyPlan -> IWeeklyPlanRepository -> WeeklyPlanRepository -> PostgreSQL
```

## Asignar una receta

1. `WeeklyPlanCalendar` emite un `MealPlanCellChange` con fecha, tipo de comida, receta y raciones.
2. `WeeklyPlanDetails.ChangeEntryAsync` decide entre asignar o retirar y conserva el estado de guardado y los mensajes visibles.
3. `WeeklyPlanApiClient.SetEntryAsync` envía `PUT /api/weekly-plans/{planId}/days/{date}/meal-types/{mealTypeId}`.
4. `WeeklyPlanEndpoints` adapta los valores de ruta y el DTO, y delega en `WeeklyPlanService`.
5. Application carga el plan, comprueba que las referencias existan y solicita la transición al agregado.
6. `WeeklyPlan` valida fecha, hueco, receta, raciones y estado antes de modificar `MealPlanEntry`.
7. `WeeklyPlanRepository` persiste el agregado mediante EF Core y devuelve el plan actualizado.
8. La respuesta vuelve al componente, que reemplaza su modelo sin acceder a repositorios ni entidades persistentes.

## Operaciones avanzadas

El mismo flujo se reutiliza para:

* Añadir, retirar y reordenar `MealPlanSlot` por día.
* Establecer una hora local opcional y devolver el inicio de preparación derivado.
* Omitir una entrada con motivo obligatorio y alternativa opcional.
* Retirar una asignación todavía planificada.

Domain impide duplicar tipos de comida en el mismo día, operar fuera de la semana o modificar elementos bloqueados. Una omisión no consume inventario; la finalización se coordina mediante `WeeklyPlanInventoryService` y se explica en [Finalización de una comida](inventory-completion-walkthrough.md).

## Dónde probar un cambio

| Cambio | Prueba principal |
|---|---|
| Fecha, raciones, orden o transición de estado | Domain |
| Referencias y cálculo de horario | Application |
| Migración, claves y round-trip del agregado | Integration con PostgreSQL |
| Ruta o `ProblemDetails` | Integration HTTP |
| Controles, borradores y errores recuperables | Component con bUnit |
| Recorrido visible de una semana | E2E con Playwright |

Antes de modificar el calendario, distingue el hueco estable (`MealPlanSlot`) de la asignación opcional (`MealPlanEntry`). Reordenar o cambiar la hora de un hueco no debe cambiar su identidad ni recrear la entrada asociada.
