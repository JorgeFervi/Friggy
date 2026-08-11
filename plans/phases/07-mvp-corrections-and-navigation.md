# Fase 7 — Correcciones del MVP y navegación

- **Estado:** Planificada
- **Estimación:** 1–2 días
- **Dependencias:** [Fase 6 completada](../completed/phases/06-stabilization-and-pilot.md)
- **Guía ejecutable:** [Implementación de la fase 7](../implementation/07-mvp-corrections-and-navigation-implementation.md)

## Resultado esperado

Cerrar las dos carencias transversales observadas durante el uso del MVP antes de modificar el dominio: eliminar recetas desde Web cuando no tengan referencias y disponer de navegación principal persistente.

## Alcance

- Acción visible para eliminar recetas con confirmación.
- Eliminación correcta de recetas no referenciadas.
- Conflicto comprensible cuando una receta esté asignada a un plan semanal.
- Navegación desde cualquier pantalla a Inicio, Recetas, Planes semanales y Catálogos.
- Espacio de navegación preparado para añadir Inventario en la Fase 8.

No cambia el esquema, las entidades de Domain ni las rutas HTTP existentes.

## Orden de ejecución

1. Reproducir con bUnit la ausencia de eliminación en la pantalla de recetas.
2. Añadir confirmación, estado de envío, manejo de error y actualización del listado.
3. Verificar por integración el conflicto de borrado cuando existe una asignación semanal.
4. Reproducir con bUnit la ausencia de navegación persistente.
5. Implementar navegación accesible y conservar el indicador de interactividad.
6. Ejecutar regresión de recetas, planes, componentes y recorrido E2E.

## Entregables

- Eliminación de recetas disponible en Web sin duplicar el contrato ya existente.
- Mensaje estable para referencias que impiden el borrado.
- Navegación principal reutilizable y accesible.
- Pruebas de regresión focalizadas.

## Criterios de salida

- Una receta sin referencias puede eliminarse tras confirmación y desaparece del listado.
- Una receta asignada no se elimina y la interfaz explica el conflicto sin perder estado.
- La navegación está disponible en todas las páginas y funciona por rutas HTTP de Web.
- No se introducen cambios de base de datos ni nuevas dependencias entre proyectos.
- `scripts/quality-gate.ps1` finaliza en verde.

## Handoff

Habilitar [Fase 8 — Inventario y finalización de comidas](08-inventory-and-meal-completion.md) sobre una base funcional y navegable.
