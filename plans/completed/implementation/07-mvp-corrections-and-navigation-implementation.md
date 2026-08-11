# Implementación ejecutable — Fase 7

- **Fase relacionada:** [Fase 7 — Correcciones del MVP y navegación](../phases/07-mvp-corrections-and-navigation.md)
- **Skills aplicables:** `code-review`, `blazor`, `xunit`, `run-tests`, `test-anti-patterns`

> **Progreso:** fase completada. Las subfases 7.1 a 7.5 están verdes; la siguiente unidad ejecutable es la Fase 8.

Esta fase no cambia Domain ni PostgreSQL. Corrige dos recorridos de Web apoyándose en contratos ya existentes y debe permanecer pequeña.

## Matriz de comportamiento

| Comportamiento | Primer rojo | Regresión |
|---|---|---|
| Eliminar receta sin referencias | Component | API e Integration existentes |
| Confirmar antes de borrar | Component | la cancelación no llama a la API |
| Mostrar conflicto por plan asignado | Component e Integration | receta y plan permanecen |
| Navegar desde cualquier página | Component | rutas existentes y estado interactivo |

## 7.1 — Congelar el baseline

1. Ejecutar `scripts/quality-gate.ps1` sobre el commit de partida.
2. Añadir un test bUnit que demuestre que el listado carece de acción de borrado.
3. Añadir un test de componente que exija navegación principal dentro del layout.
4. Confirmar que los rojos se deben únicamente a comportamiento ausente.

No modificar contratos HTTP ni añadir migraciones.

## 7.2 — Eliminar recetas desde Web

1. Probar confirmación, cancelación, envío único y estado deshabilitado.
2. Usar `IRecipesApiClient.DeleteAsync`; no crear una segunda ruta ni servicio.
3. Tras éxito, retirar la receta del listado o recargarlo de forma observable.
4. Conservar el listado y mostrar error recuperable si la API falla.

El test debe comprobar efecto y número de llamadas, no solo la presencia del botón.

## 7.3 — Conflictos por referencias

1. Añadir integración con PostgreSQL: una receta asignada a un plan no se elimina.
2. Verificar `409 ProblemDetails` con código estable, sin exponer el mensaje de EF Core.
3. Añadir bUnit para el mensaje comprensible y la conservación de la fila.
4. Verificar que una receta sin referencias sigue eliminándose.

## 7.4 — Navegación principal

1. Añadir navegación semántica al layout con enlaces a Inicio, Recetas, Planes y Catálogos.
2. Mantener `@Body`, el indicador de interactividad y el manejo global de error.
3. Probar nombres accesibles, destinos y disponibilidad desde una página distinta de Inicio.
4. Preparar un punto de extensión para Inventario sin mostrar todavía una ruta inexistente.

No introducir estado global, JavaScript ni una librería visual para este menú.

## 7.5 — Regresión y cierre

1. Ejecutar tests focalizados de componentes de recetas y layout.
2. Ejecutar Integration y Component completas.
3. Añadir o ampliar un E2E que elimine una receta no utilizada y navegue sin volver atrás.
4. Ejecutar `scripts/quality-gate.ps1`.
5. Actualizar el estado de la fase y habilitar la Fase 8 solo con el gate verde.
