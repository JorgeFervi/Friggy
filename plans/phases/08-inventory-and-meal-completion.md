# Fase 8 — Inventario y finalización de comidas

- **Estado:** Planificada
- **Estimación:** 5–8 días
- **Dependencias:** [Fase 7](07-mvp-corrections-and-navigation.md) y [decisiones de inventario](../discovery/07-inventory-decisions.md)
- **Guía ejecutable:** [Implementación de la fase 8](../implementation/08-inventory-and-meal-completion-implementation.md)

## Resultado esperado

Registrar inventario por lotes con caducidad e historial, calcular las carencias de un plan semanal seleccionado y consumir manualmente existencias al completar cada comida.

## Alcance

- Una receta representa una ración; cada asignación semanal define raciones positivas.
- Lotes con ingrediente, unidad, cantidad actual y caducidad obligatoria.
- Movimientos de alta, consumo, ajuste y descarte.
- Exclusión de lotes caducados del stock utilizable.
- Coincidencia exacta por ingrediente y unidad, sin conversiones.
- Requerido, disponible y faltante para el plan seleccionado.
- Finalización irreversible de una comida en este incremento.
- Selección manual de uno o varios lotes y consumo parcial sin stock negativo.
- Protección de ingredientes y unidades referenciados.

## Límites

- No se descuentan existencias al planificar.
- No hay ubicaciones, lista de la compra ni equivalencias de unidad.
- Una comida completada no se reabre; las correcciones usan ajustes de inventario.
- Los lotes agotados y caducados se conservan para consulta, ocultos por defecto del disponible.

## Orden de ejecución

1. Añadir raciones a la planificación con compatibilidad para datos existentes.
2. Implementar lotes, operaciones e invariantes en Domain.
3. Añadir puertos, servicios y contratos de Application.
4. Crear migración, configuraciones y repositorio por agregado con PostgreSQL real.
5. Exponer inventario y movimientos mediante Minimal APIs.
6. Calcular necesidades y carencias del plan seleccionado.
7. Completar una comida y guardar consumos de forma atómica e idempotente.
8. Crear pantallas de inventario y ampliar el detalle del plan semanal.
9. Verificar el recorrido completo con Playwright.

## Criterios de salida

- Nunca se persiste una cantidad negativa.
- Cada cambio de cantidad deja exactamente un movimiento explicativo.
- Los lotes utilizables se ordenan por caducidad ascendente para la selección manual.
- El cálculo semanal agrega líneas repetidas y multiplica por raciones.
- Completar dos veces la misma comida no duplica movimientos.
- La operación de completado no deja estados parciales ante un error.
- Migración desde la Fase 7 y base vacía verificadas con PostgreSQL.
- Las cinco suites y el quality gate están en verde.

## Handoff

Habilitar [Fase 9 — Planificación semanal avanzada](09-advanced-weekly-planning.md) con raciones, finalización e historial disponibles.
