# Estado y hoja de ruta

> **Estado:** vigente · **Última revisión:** 23 de agosto de 2026

Las fases 1 a 16 están implementadas en el código actual. El detalle histórico y ejecutable se conserva en [planes](../../plans/README.md) y en la [fase 16](../plans/phases/16-unit-conversions.md).

## Capacidades entregadas

* Catálogos de ingredientes, unidades, etiquetas y tipos de comida.
* Recetas con ingredientes, pasos, clasificación y asociación de ingredientes a pasos.
* Planificación diaria por intervalos libres, con raciones, horarios, omisiones y finalización.
* Inventario por lotes, caducidad, movimientos y cálculo de carencias.
* Plantillas diarias y lista de la compra calculada por intervalo.
* Conversiones de masa, volumen y conteo con unidades diferenciadas para cocina y compra.
* Interfaz responsive, accesible y protegida mediante regresión visual.
* Automatización local, cinco niveles de pruebas y puerta de calidad.

La descripción funcional vigente está en [Funcionalidades actuales](current-capabilities.md).

## Incrementos entregados recientemente

* [Fase 13: plantillas de planes diarios](../../plans/phases/13-daily-plan-templates.md).
* [Fase 14: lista de la compra por intervalo](../../plans/phases/14-shopping-list.md).
* [Fase 15: consolidación de UI y UX](../../plans/phases/15-ui-ux-consolidation.md).
* [Fase 16: conversiones y usos contextuales de unidades](../plans/phases/16-unit-conversions.md).

## Posibles evoluciones

Las siguientes ideas no constituyen un compromiso ni tienen orden o fecha asignados:

* Cuentas de usuario, autenticación y aislamiento de datos.
* Densidades por ingrediente y equivalencias entre masa y volumen.
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
