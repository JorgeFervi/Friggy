# Fase 11 — Panel principal y analítica

- **Estado:** Futura
- **Estimación:** 4–6 días
- **Dependencias:** [Fase 8](08-inventory-and-meal-completion.md), [Fase 9](09-advanced-weekly-planning.md) y [Fase 10](10-recipe-step-ingredients.md)
- **Guía ejecutable:** [Implementación de la fase 11](../implementation/11-dashboard-and-analytics-implementation.md)

## Resultado esperado

Convertir la pantalla principal en un panel que resuma el plan seleccionado mediante datos ya confirmados, con navegación rápida y visualizaciones accesibles.

## Alcance del primer incremento

- Próxima comida según fecha y hora planificadas.
- Carencias de inventario de la semana seleccionada.
- Calendario para abrir rápidamente un día del plan.
- Comidas completadas, omitidas y pendientes.
- Totales de comidas por etiquetas de receta.
- Recetas más utilizadas según comidas completadas.
- Ingredientes más utilizados según consumos vinculados a comidas completadas.

Todos los indicadores usan inicialmente el plan semanal seleccionado como periodo. Las vistas históricas y filtros arbitrarios quedan para una evolución posterior.

## Principios de consulta

- Application define DTO y puertos de lectura; Infrastructure proyecta sin cargar agregados completos.
- La interfaz no recalcula reglas de inventario ni estadísticas.
- No se añade un almacén analítico, eventos, caché o trabajos en segundo plano sin medición.
- Cada gráfico ofrece también título, valores y alternativa textual o tabular.

## Criterios de salida

- Un panel vacío es comprensible y permite seleccionar o crear un plan.
- Todos los indicadores se pueden reconciliar con el detalle de planes, recetas y movimientos.
- Próxima comida ignora elementos completados u omitidos.
- Los ingredientes descartados o ajustados no cuentan como consumidos por una receta.
- Las consultas relevantes se miden sobre PostgreSQL y evitan N+1.
- Componentes, API, integración, accesibilidad y recorrido E2E están en verde.

## Handoff

Evaluar después, con uso real, filtros históricos, lista de la compra y visualizaciones adicionales como fases independientes.
