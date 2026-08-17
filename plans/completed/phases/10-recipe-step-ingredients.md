# Fase 10 — Ingredientes asociados a pasos

- **Estado:** Completada el 13 de agosto de 2026
- **Estimación:** 2–4 días
- **Dependencias:** [Fase 9 completada](09-advanced-weekly-planning.md)
- **Guía ejecutable:** [Implementación de la fase 10](../implementation/10-recipe-step-ingredients-implementation.md)

Domain, Application, Infrastructure, API y Web conservan las asociaciones por identidad, protegen en PostgreSQL que ambos extremos pertenezcan a la misma receta y permiten editarlas y consultarlas. El recorrido completo y la invariancia del inventario quedaron verificados por la puerta de calidad.

## Resultado esperado

Indicar en cada paso qué líneas de ingrediente de la receta se utilizan, sin duplicar cantidades ni alterar el cálculo de inventario.

## Alcance

- Relación entre un paso y cero o varias líneas de ingrediente de su propia receta.
- Una misma línea puede utilizarse en varios pasos.
- El formulario permite seleccionar ingredientes al editar cada paso.
- El detalle presenta los ingredientes asociados junto a la instrucción.
- Eliminar una línea o un paso elimina sus asociaciones de forma controlada.

Las cantidades siguen perteneciendo a `RecipeIngredient`. La relación por paso es descriptiva y no reparte cantidades en este incremento.

## Criterios de salida

- No se puede asociar un paso con una línea de otra receta.
- No existen asociaciones duplicadas para el mismo paso y línea.
- Reordenar pasos o ingredientes conserva las asociaciones por identidad.
- Crear, editar y consultar recetas mantiene compatibilidad con recetas anteriores sin asociaciones.
- Migración, API, formulario, detalle y suites afectadas están en verde.

## Handoff

La fase deja disponible información de preparación más rica sin cambiar las necesidades totales del inventario.
