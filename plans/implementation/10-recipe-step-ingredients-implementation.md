# Implementación ejecutable — Fase 10

- **Fase relacionada:** [Fase 10 — Ingredientes asociados a pasos](../phases/10-recipe-step-ingredients.md)
- **Skills aplicables:** `architecture`, `modern-csharp`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`

La asociación referencia `RecipeIngredient`, no el catálogo `Ingredient`, para distinguir líneas repetidas con unidades o cantidades diferentes.

## 10.1 — Invariantes en Domain

1. Escribir rojos para asociar y retirar una línea de ingrediente de un paso.
2. Impedir duplicados y referencias a otra receta.
3. Permitir cero asociaciones para compatibilidad.
4. Definir eliminación consistente al retirar un paso o una línea.

No repartir ni duplicar cantidades en los pasos.

## 10.2 — Contratos y actualización de receta

1. Ampliar solicitudes y respuestas de paso con IDs de líneas de ingrediente.
2. Validar referencias después de construir la identidad de las nuevas líneas.
3. Mantener reemplazo de receta atómico y preservar asociaciones válidas.
4. Probar alta, edición, reordenación y errores con fakes manuales.

## 10.3 — Persistencia y migración

1. Crear tabla de unión con clave o índice único por paso y línea.
2. Configurar cascada solo dentro del agregado de receta.
3. Probar FK cruzada inválida y borrado de paso/línea en PostgreSQL.
4. Verificar recetas anteriores sin asociaciones.

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
