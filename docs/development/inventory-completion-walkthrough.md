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
3. Application multiplica las líneas por raciones y normaliza cada cantidad por ingrediente y dimensión de medida.
4. Los lotes con cantidad positiva y caducidad igual o posterior a hoy se normalizan mediante la misma política.
5. Masa, volumen y conteo se agregan en su unidad base; las unidades `Unconverted` conservan la coincidencia exacta por `UnitTypeId`.
6. El servicio elige de forma determinista una unidad habilitada para compra y devuelve requerido, disponible y faltante sin persistir el cálculo.

## Completar una comida

1. Se presentan los lotes inventariados compatibles y se construye `CompleteMealRequest` con cantidades explícitas.
2. `DailyPlanApiClient` envía `POST /api/daily-plans/{date}/meal-types/{mealTypeId}/complete`.
3. Application carga la receta, calcula sus necesidades y obtiene los lotes compatibles para su uso y actualización.
4. Valida cantidades positivas, existencia, caducidad, coincidencia de ingrediente, compatibilidad de dimensión y que la cantidad normalizada no supere lo requerido.
5. Cada `InventoryLot.Consume` crea un movimiento enlazado con la entrada finalizada, conserva la unidad original del lote y evita cantidades negativas.
6. `DailyPlan.CompleteEntry` cierra la asignación.
7. `InventoryUnitOfWork.SaveChangesAsync` persiste movimientos, lotes y plan con el mismo `DbContext`; un conflicto de concurrencia se traduce a un error recuperable.
8. La respuesta indica consumos aplicados y necesidades restantes en una unidad de compra compatible.

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
