# Recorrido técnico — Finalización de una comida

> **Estado:** vigente

## Objetivo

La finalización une planificación e inventario y debe conservar atomicidad, idempotencia y trazabilidad.

```text
DailyPlanDetails -> DailyPlanApiClient -> DailyPlanEndpoints
    -> DailyPlanInventoryService
    -> DailyPlan + InventoryLot
    -> IInventoryUnitOfWork -> FriggyDbContext -> PostgreSQL
```

## Calcular necesidades de ingredientes de un plan diario

1. `DailyPlanDetails` solicita `GET /api/daily-plans/{date}/inventory-requirements`.
2. `DailyPlanInventoryService.GetRequirementsAsync` carga el plan de la fecha y las recetas asignadas.
3. Application agrupa líneas por `(IngredientId, UnitTypeId)` y multiplica por raciones.
4. Los lotes con cantidad positiva y caducidad igual o posterior a hoy se agrupan por la misma clave.
5. El servicio devuelve la cantidad de ingredientes requerido, disponible y faltante sin persistir el cálculo.

## Completar una comida

1. Se presentan los lotes inventariados compatibles y se construye `CompleteMealRequest` con cantidades explícitas.
2. `DailyPlanApiClient` envía `POST /api/daily-plans/{date}/meal-types/{mealTypeId}/complete`.
3. `Application` Carga la receta, calcula sus necesidades y obtiene los lotes compatibles para su uso y actualización con la receta seleccionada.
5. Valida cantidades positivas, existencia, caducidad, coincidencia exacta de ingrediente/unidad y que no se supere lo requerido.
6. Cada `InventoryLot.Consume` crea un movimiento enlazado con la entrada de la receta finalizada y evita cantidad negativa.
7. `DailyPlan.CompleteEntry` cierra la asignación.
8. `InventoryUnitOfWork.SaveChangesAsync` persiste movimientos, lotes y plan con el mismo `DbContext`; un conflicto de concurrencia se traduce a un error recuperable.
9. La respuesta indica consumos aplicados y necesidades restantes.

## Invariantes que no deben incumplirse

* Repetir una finalización no duplica consumos.
* Un lote caducado, inexistente o de otra necesidad se rechaza.
* Una comida omitida nunca puede completarse.
* Una cantidad solicitada no puede superar la necesidad de cantidad total.
* El stock parcial puede completar la comida e informar del resto pendiente.
* Las correcciones posteriores usan ajustes de lote y no reabren la entrada.

## Pruebas mínimas

| Riesgo | Prueba principal |
|---|---|
| Total, raciones y fecha límite | Application con reloj controlado |
| Cantidad negativa o movimiento inconsistente | Domain |
| Atomicidad, concurrencia y rollback | Integration con PostgreSQL |
| Códigos HTTP y `ProblemDetails` | Integration HTTP |
| Selección de lotes y mensajes | Component con bUnit |
| Consumo y persistencia tras reinicio | E2E con Playwright |
