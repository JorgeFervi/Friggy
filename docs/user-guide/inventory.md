# Inventario doméstico

> **Estado:** vigente

Friggy registra las existencias por lotes para conservar cantidades, caducidades e historial de movimientos de forma independiente.

## Añadir un lote

En **Inventario**, selecciona:

* Ingrediente.
* Unidad.
* Cantidad inicial positiva.
* Fecha de caducidad.

El alta crea automáticamente el primer movimiento del historial. Los lotes disponibles se ordenan por caducidad y los agotados o caducados permanecen ocultos hasta activar **Mostrar agotados y caducados**.

## Estado del lote

* **Disponible:** tiene cantidad positiva y su caducidad no ha pasado.
* **Agotado:** su cantidad es cero.
* **Caducado:** la fecha de caducidad es anterior al día actual.

Un lote que caduca hoy todavía puede utilizarse.

## Operaciones

Abre el detalle de un lote para realizar estas operaciones:

* **Consumir:** resta la cantidad realmente utilizada.
* **Descartar:** resta una cantidad que se tira o deja de estar disponible.
* **Ajustar a cantidad real:** sustituye la cantidad registrada por el valor contado físicamente.
* **Corregir caducidad:** cambia una fecha registrada incorrectamente.

Consumir o descartar más de lo disponible aplica únicamente la cantidad posible e informa del resto no registrado. La cantidad nunca se vuelve negativa.

## Historial

Cada cambio de cantidad registra tipo, diferencia, cantidad resultante y momento. Los movimientos se muestran del más reciente al más antiguo y no se editan ni eliminan.

Las comidas completadas generan movimientos de consumo enlazados con su asignación del plan diario. Una corrección posterior debe registrarse como ajuste del lote para conservar la trazabilidad.
