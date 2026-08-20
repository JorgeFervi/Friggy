# Planificación semanal

> **Estado:** vigente

## Crear un plan

Un plan semanal tiene nombre, descripción opcional y una fecha de inicio que debe ser lunes. Siempre representa siete días consecutivos.

Al abrir su detalle puedes editar nombre y descripción, asignar recetas y revisar las necesidades de inventario.

## Configurar cada día

Los huecos de comida pertenecen a un día concreto. Puedes:

* Añadir un tipo de comida que todavía no exista en ese día.
* Mover un hueco hacia arriba o abajo.
* Retirar un hueco que no esté bloqueado.
* Asignar o retirar una receta.
* Cambiar el número de raciones por un entero positivo.
* Definir o eliminar una hora prevista.

Cuando hay receta y hora, Friggy deriva el inicio de preparación restando el tiempo estimado. La hora es local y no se convierte entre zonas horarias.

## Necesidades de inventario

El resumen agrupa las cantidades por combinación exacta de ingrediente y unidad. Multiplica cada línea por las raciones y excluye:

* Comidas sin receta.
* Comidas omitidas.
* Lotes agotados.
* Lotes cuya caducidad ya ha pasado.

La fecha de caducidad de hoy todavía se considera utilizable. No hay conversiones entre unidades ni generación automática de lista de la compra.

## Omitir una comida

Una asignación planificada puede marcarse como omitida indicando un motivo obligatorio y una alternativa opcional. La receta queda conservada como historial, no consume lotes y deja de contar en las necesidades.

La omisión es irreversible desde la interfaz. Si se prepara otra receta registrada y se quiere consumir inventario, sustituye primero la asignación y utiliza el flujo normal de finalización.

## Completar una comida

Selecciona **Completar** y reparte el consumo entre los lotes compatibles. Cada selección debe corresponder al ingrediente y unidad requeridos y no puede usar un lote caducado.

La operación completa la asignación y registra todos los movimientos en una sola transacción. Puede informar de necesidad restante si el stock es parcial. Una comida completada queda bloqueada; las correcciones posteriores se realizan ajustando los lotes, no repitiendo la finalización.
