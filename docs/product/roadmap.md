# Estado y hoja de ruta

> **Estado:** vigente · **Última revisión:** 22 de agosto de 2026

Las fases 1 a 12 están completadas. Las fases 13 y 14 están planificadas. El detalle histórico y futuro se conserva en [planes](../../plans/README.md).

## Capacidades entregadas

* Catálogos de ingredientes, unidades, etiquetas y tipos de comida.
* Recetas con ingredientes, pasos, clasificación y asociación de ingredientes a pasos.
* Planificación diaria por intervalos libres, con raciones, horarios, omisiones y finalización.
* Inventario por lotes, caducidad, movimientos y cálculo de carencias.
* Interfaz responsive, accesible y protegida mediante regresión visual.
* Automatización local, cinco niveles de pruebas y puerta de calidad.

La descripción funcional vigente está en [Funcionalidades actuales](current-capabilities.md).

## Incrementos planificados

Estos planes describen trabajo futuro y no cambian todavía las funcionalidades actuales:

* [Fase 13: crear y aplicar plantillas de planes diarios](../../plans/phases/13-daily-plan-templates.md).
* [Fase 14: calcular una lista de la compra por intervalo](../../plans/phases/14-shopping-list.md).

La [Fase 12](../../plans/completed/phases/12-daily-planning.md) ya proporciona la base diaria estable. Plantillas y lista de la compra son incrementos independientes.

## Posibles evoluciones

Las siguientes ideas no constituyen un compromiso ni tienen orden o fecha asignados:

* Cuentas de usuario, autenticación y aislamiento de datos.
* Conversiones controladas entre unidades compatibles.
* Imágenes de recetas, pasos o ingredientes.
* Información nutricional.
* Sugerencias y asistencia mediante inteligencia artificial.
* Despliegue público y operación de producción.

## Criterios para iniciar una fase

Antes de incorporar una capacidad nueva se debe:

1. Definir el problema del usuario y el recorrido de aceptación.
2. Decidir expresamente privacidad, propiedad de datos y compatibilidad con el modo local.
3. Identificar cambios de dominio, contratos, persistencia, API e interfaz.
4. Preparar el plan TDD y los niveles de prueba necesarios.
5. Registrar un ADR cuando cambie una decisión arquitectónica o tecnológica.

Los documentos históricos de [casos de uso](../use-cases/000-general-ideas.md) explican el origen de varias ideas, pero esta página es la fuente de su estado actual.
