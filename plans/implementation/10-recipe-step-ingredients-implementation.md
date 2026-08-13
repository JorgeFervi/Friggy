# Implementación ejecutable — Fase 10

- **Fase relacionada:** [Fase 10 — Ingredientes asociados a pasos](../phases/10-recipe-step-ingredients.md)
- **Skills aplicables:** `architecture`, `modern-csharp`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`

La asociación referencia `RecipeIngredient`, no el catálogo `Ingredient`, para distinguir líneas repetidas con unidades o cantidades diferentes.

> **Progreso:** 10.1–10.3 completadas. La siguiente unidad ejecutable es **10.4 — API**.

## 10.1 — Invariantes en Domain — Completada

1. Escribir rojos para asociar y retirar una línea de ingrediente de un paso.
2. Impedir duplicados y referencias a otra receta.
3. Permitir cero asociaciones para compatibilidad.
4. Definir eliminación consistente al retirar un paso o una línea.

No repartir ni duplicar cantidades en los pasos.

`Recipe` administra las asociaciones mediante la identidad de `RecipeStep` y `RecipeIngredient`, rechazando elementos ajenos al agregado y duplicados. Los pasos pueden permanecer sin asociaciones; retirar una línea o un paso limpia sus vínculos, y la cantidad continúa perteneciendo exclusivamente a la línea de ingrediente. La suite Domain quedó verde con 115/115 pruebas y la regresión focalizada de persistencia de recetas con PostgreSQL superó 6/6 pruebas.

## 10.2 — Contratos y actualización de receta — Completada

1. Ampliar solicitudes y respuestas de paso con IDs de líneas de ingrediente.
2. Validar referencias después de construir la identidad de las nuevas líneas.
3. Mantener reemplazo de receta atómico y preservar asociaciones válidas.
4. Probar alta, edición, reordenación y errores con fakes manuales.

Las solicitudes aceptan una identidad opcional por línea y asociaciones opcionales por paso, conservando compatibilidad con clientes anteriores. Application construye primero todas las líneas y después valida los vínculos; las respuestas siempre exponen IDs ordenados por la posición actual de los ingredientes. `ReplaceWith` conserva identidades válidas, reutiliza líneas ya seguidas por persistencia y solo sustituye el agregado tras preparar el nuevo estado. Quedaron verdes Domain (115/115), Application (70/70), Component (73/73) y la regresión focalizada de endpoints con PostgreSQL (6/6).

## 10.3 — Persistencia y migración — Completada

1. Crear tabla de unión con clave o índice único por paso y línea.
2. Configurar cascada solo dentro del agregado de receta.
3. Probar FK cruzada inválida y borrado de paso/línea en PostgreSQL.
4. Verificar recetas anteriores sin asociaciones.

La migración `AddRecipeStepIngredients` crea una tabla de unión vacía con unicidad por paso y línea, y FKs compuestas que impiden enlazar elementos de recetas diferentes. Las cascadas solo parten de `RecipeStep` y `RecipeIngredient`; el repositorio carga las asociaciones mediante una consulta separada constante y conserva `AsNoTrackingWithIdentityResolution` en listados. Se verificaron bases vacías, upgrade desde Fase 9, round-trip, referencias cruzadas, borrado de extremos y ausencia de N+1. La suite Integration quedó verde con 75/75 pruebas y el snapshot no tiene cambios pendientes.

## 10.4 — API

1. Mantener las rutas de recetas y ampliar sus DTO.
2. Comprobar que create/update/get hacen round-trip de asociaciones ordenadas.
3. Devolver validación estable para IDs que no pertenecen a la receta.

## 10.5 — Formulario y detalle

1. Añadir selección múltiple accesible de líneas de ingrediente en cada paso.
2. Actualizar opciones al añadir o retirar ingredientes sin conservar IDs inválidos.
3. Mostrar nombre y unidad en detalle para diferenciar líneas repetidas.
4. Cubrir edición, reordenación y estado vacío con bUnit.

## 10.6 — Gate

1. E2E: crear receta, asociar líneas a pasos, editar y volver a consultar.
2. Verificar que las cantidades de inventario requeridas no cambian por estas asociaciones.
3. Ejecutar todas las suites y `scripts/quality-gate.ps1`.
