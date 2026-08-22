# Recorrido técnico — Planificación diaria

> **Estado:** vigente

```text
DailyPlanEditor -> DailyPlanDetails -> DailyPlanApiClient
    -> DailyPlanEndpoints -> DailyPlanService
    -> DailyPlan -> IDailyPlanRepository -> DailyPlanRepository -> PostgreSQL
```

La fecha identifica el recurso HTTP (`/api/daily-plans/{date}`) y solo vive en la raíz `DailyPlan`. Los huecos y asignaciones referencian la raíz mediante `DailyPlanId`, sin duplicar la fecha.

Application valida intervalos inclusivos, referencias y estados. Domain protege la unicidad de tipos de comida, raciones positivas y entradas cerradas. Infrastructure aplica además un índice único sobre `daily_plans.date`, por lo que la carrera entre dos altas se traduce al conflicto estable `daily-plan.date.duplicate`.

Las pruebas de Domain cubren invariantes; Application usa fakes manuales; Integration comprueba PostgreSQL, migración y HTTP; bUnit cubre componentes y Playwright los recorridos visibles.
