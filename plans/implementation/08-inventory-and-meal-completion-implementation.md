# Implementación ejecutable — Fase 8

- **Fase relacionada:** [Fase 8 — Inventario y finalización de comidas](../phases/08-inventory-and-meal-completion.md)
- **Decisiones vinculantes:** [Inventario doméstico](../discovery/07-inventory-decisions.md)
- **Skills aplicables:** `architecture`, `modern-csharp`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`, `run-tests`

Inventario se incorpora como capacidad dentro de los proyectos actuales. `InventoryLot` es la raíz que protege su cantidad y movimientos; no se crea un repositorio independiente por movimiento.

## Matriz de riesgos

| Riesgo | Protección principal |
|---|---|
| Cantidad negativa | invariantes Domain y constraint PostgreSQL |
| Movimiento sin cambio o cambio sin movimiento | operación atómica y tests Integration |
| Doble consumo al reintentar | finalización idempotente por entrada semanal |
| Stock caducado contado | fecha controlada en Application y escenarios límite |
| Conversiones accidentales | agrupación exacta por ingrediente y unidad |
| Pérdida de referencias | FK restrict para ingrediente y unidad |

## 8.1 — Raciones en la planificación

1. Escribir rojos de Domain para raciones enteras mayores que cero y valor inicial uno.
2. Ampliar contratos de asignación y respuesta sin cambiar la identidad de la celda.
3. Crear migración con valor uno para entradas existentes y constraint positivo.
4. Probar actualización de raciones en Application, API, PostgreSQL y calendario Blazor.

Ejecutar Domain, Application, Integration y Component antes de continuar.

## 8.2 — Lote y movimientos en Domain

1. Definir lotes con ingrediente, unidad, cantidad actual y `DateOnly` de caducidad.
2. Empezar por rojos para alta positiva, consumo, ajuste por cantidad real y descarte.
3. Registrar un movimiento por cada cambio, con tipo, diferencia, cantidad resultante y momento.
4. Conservar movimientos como colección de solo lectura; no exponer edición o borrado.
5. Probar consumo y descarte parciales, agotamiento y rechazo de IDs o cantidades inválidas.

La alta crea el lote y su movimiento inicial en una misma construcción válida.

## 8.3 — Application y fakes

1. Definir DTO de lote, detalle, movimiento y solicitudes de operación.
2. Introducir un puerto por agregado para cargar y guardar lotes completos.
3. Implementar casos de uso de alta, consulta, corrección de caducidad, consumo, ajuste y descarte.
4. Inyectar una abstracción mínima de fecha/hora donde la caducidad o el historial dependan del reloj.
5. Probar cancelación, no encontrado, referencias inexistentes y resultados parciales con fakes manuales.

Application devuelve cantidades aplicadas y no registradas para consumos o descartes parciales.

## 8.4 — PostgreSQL y migración

1. Crear tablas de lotes y movimientos con precisión coherente con las cantidades de receta.
2. Añadir FK restrict, cantidad no negativa, movimiento no nulo e índices de consulta por ingrediente, unidad y caducidad.
3. Impedir modificar o borrar movimientos mediante los casos de uso publicados.
4. Probar migración desde el esquema de Fase 7 y desde base vacía.
5. Probar dos operaciones concurrentes para demostrar que no hay pérdida de actualización ni stock negativo.

La estrategia de concurrencia debe ser explícita y devolver conflicto recuperable.

## 8.5 — API e interfaz de inventario

1. Exponer rutas agrupadas para listar, obtener, crear y operar sobre lotes.
2. Publicar `ProblemDetails` estable para validación, no encontrado, referencia y concurrencia.
3. Crear cliente HTTP tipado y pantallas de disponible, agotado/caducado y detalle con historial.
4. Ordenar disponible por caducidad ascendente y ocultar agotados por defecto.
5. Probar estados vacío, loading, error, operación parcial y confirmaciones con bUnit.

## 8.6 — Carencias del plan seleccionado

1. Escribir tests de Application para agrupar líneas repetidas y multiplicar por raciones.
2. Consultar únicamente lotes no caducados y con cantidad positiva.
3. Comparar por la clave exacta `(IngredientId, UnitTypeId)`.
4. Devolver requerido, disponible y faltante sin persistir el resultado.
5. Probar fecha de caducidad de ayer, hoy y mañana con un reloj controlado.
6. Mostrar el resultado en el detalle del plan semanal.

No implementar lista de la compra ni conversiones.

## 8.7 — Completar una comida

1. Añadir estado de completada a `MealPlanEntry` y rojos contra doble finalización.
2. Preparar la solicitud con asignaciones explícitas de lote y cantidad por necesidad.
3. Validar que cada lote corresponde al ingrediente y unidad requeridos, no está caducado y tiene cantidad utilizable.
4. Aplicar consumos parciales, crear movimientos y completar la entrada en una sola transacción.
5. Ante cualquier error no controlado, no completar ni conservar movimientos parciales.
6. Mantener la comida cerrada; las correcciones posteriores usan ajustes de lote.

Probar reintento, selección de varios lotes, falta parcial, concurrencia y cancelación.

## 8.8 — Recorrido vertical y gate

1. E2E: crear inventario, planificar raciones, consultar carencias, completar seleccionando lotes y verificar historial.
2. Reiniciar API/Web y comprobar persistencia de lote, movimientos y estado completado.
3. Ejecutar medición focalizada de las consultas de disponible y carencias.
4. Ejecutar `scripts/quality-gate.ps1`.
5. Actualizar documentación y habilitar Fase 9 solo con cero defectos bloqueantes.
