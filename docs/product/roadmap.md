# Estado y hoja de ruta

> **Estado:** vigente · **Última revisión:** 20 de agosto de 2026

Las fases 1 a 11 están completadas. No existe una fase activa; cualquier incremento nuevo debe comenzar con una decisión funcional y un plan TDD propios. El detalle histórico se conserva en [planes](../../plans/README.md).

## Capacidades entregadas

* Catálogos de ingredientes, unidades, etiquetas y tipos de comida.
* Recetas con ingredientes, pasos, clasificación y asociación de ingredientes a pasos.
* Planificación semanal con raciones, huecos por día, horarios, omisiones y finalización.
* Inventario por lotes, caducidad, movimientos y cálculo de carencias.
* Interfaz responsive, accesible y protegida mediante regresión visual.
* Automatización local, cinco niveles de pruebas y puerta de calidad.

La descripción funcional vigente está en [Funcionalidades actuales](current-capabilities.md).

## Posibles evoluciones

Las siguientes ideas no constituyen un compromiso ni tienen orden o fecha asignados:

* Cuentas de usuario, autenticación y aislamiento de datos.
* Lista de la compra a partir de carencias revisadas por el usuario.
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
