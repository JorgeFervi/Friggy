# Fase 12 — Planificación diaria

- **Estado:** En implementación
- **Estimación:** 5–7 días
- **Dependencias:** Fases 1–11 completadas
- **Guía ejecutable:** [Implementación de la fase 12](../implementation/12-daily-planning-implementation.md)

## Resultado esperado

Sustituir la planificación semanal por planes diarios independientes. Cada fecha puede tener como máximo un `DailyPlan`; el usuario consulta cualquier intervalo y crea únicamente los días que quiere planificar. Cada plan conserva huecos de comida, recetas, comensales, horarios y estados de finalización u omisión.

## Decisiones vinculantes

- `DailyPlan` es la raíz del agregado y `Date` su identidad funcional; un índice único impide dos planes para la misma fecha.
- El identificador técnico sigue siendo `Guid`, pero la API direcciona el recurso por fecha (`yyyy-MM-dd`).
- `MealPlanSlot` y `MealPlanEntry` pasan al contexto `DailyPlans`, dependen de `DailyPlanId` y dejan de guardar una fecha redundante.
- Un plan nuevo recibe los tipos de comida existentes como huecos iniciales; después pueden añadirse, retirarse y ordenarse como ahora.
- Las reglas actuales de raciones, hora prevista, inicio de preparación, omisión y finalización se conservan.
- Un plan con comidas completadas no se puede eliminar, porque los movimientos de inventario mantienen trazabilidad hacia la entrada.
- No se migrarán datos funcionales de planes semanales. La migración de esquema será explícitamente destructiva para esas tablas y se probará desde el esquema actual y desde una base vacía.
- Las migraciones históricas y los planes completados no se reescriben. La aplicación activa, sus pruebas, rutas, navegación y documentación vigente no conservarán conceptos semanales.

## Alcance

1. Modelo diario y códigos de error `daily-plan.*` en Domain.
2. Casos de uso para consultar un intervalo inclusivo, obtener o crear un día, eliminarlo y modificar sus comidas.
3. Repositorio por agregado, configuración EF Core y migración PostgreSQL.
4. Rutas `/api/daily-plans` y adaptación de la finalización de comidas.
5. Experiencia Blazor basada en intervalo de fechas, con días planificados y no planificados.
6. Sustitución de nombres, clientes, componentes, pruebas, textos, documentación viva y baselines visuales semanales.

## Fuera de alcance

- Plantillas, que pertenecen a la [Fase 13](13-daily-plan-templates.md).
- Lista de la compra por rango, que pertenece a la [Fase 14](14-shopping-list.md).
- Recurrencias, conversiones de unidades, autenticación o migración de datos semanales existentes.

## Orden de ejecución

1. Congelar con tests el comportamiento que debe sobrevivir al cambio: huecos, asignaciones, comensales, horarios, omisiones y finalización atómica.
2. Crear `DailyPlan` y adaptar sus hijos en Domain, empezando por rojos de unicidad lógica y transiciones.
3. Sustituir puertos, DTO y servicios de Application; añadir la consulta inclusiva por intervalo.
4. Crear el nuevo modelo EF Core y la migración destructiva revisable con PostgreSQL real.
5. Publicar la API diaria y actualizar el enlace de inventario sin compatibilidad temporal con rutas semanales.
6. Sustituir la interfaz por un explorador de fechas y una tarjeta o detalle de un único día.
7. Renombrar o retirar toda referencia semanal activa, actualizar documentación y revisar las nuevas capturas visuales.
8. Ejecutar el gate completo y cerrar únicamente con cero pruebas omitidas.

## Criterios de aceptación

- Es posible consultar cualquier intervalo válido e inclusivo, incluidos un solo día y semanas incompletas.
- El usuario crea planes únicamente para las fechas elegidas y nunca obtiene dos planes para el mismo día, incluso con peticiones concurrentes.
- Cada comida admite receta, comensales enteros positivos, hora prevista, omisión y finalización con las reglas actuales.
- La finalización consume lotes y guarda el estado en una sola transacción; el reintento sigue siendo idempotente.
- Los días sin plan se distinguen de los planes sin recetas.
- `Friggy.Web` solo usa clientes HTTP y no introduce referencias a Infrastructure.
- La migración se verifica desde el último esquema y desde una base vacía.
- No quedan rutas, tipos, namespaces, pantallas ni pruebas activas denominadas `WeeklyPlan` o `weekly-plans`.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Perder la trazabilidad de consumos | conservar `MealPlanEntry.Id` como destino de `InventoryMovement` y bloquear el borrado de planes completados |
| Duplicar días por concurrencia | índice único sobre `daily_plans.date` y traducción de la colisión a `409 Conflict` |
| Reintroducir fecha inconsistente en hijos | la fecha vive solo en `DailyPlan`; slots y entradas usan únicamente `DailyPlanId` |
| Reescribir migraciones históricas | añadir una migración nueva y limitar la limpieza a código, snapshot y documentación vigentes |
| Componente diario demasiado grande | separar página contenedora, selector de intervalo y calendario/tarjeta presentacional |

## Handoff

La fase termina con una planificación diaria estable que sirve como única fuente de fechas y comidas para plantillas y lista de la compra.
