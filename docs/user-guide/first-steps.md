# Tu primera semana con Friggy

> **Estado:** vigente · **Duración aproximada:** 10 minutos

Este tutorial recorre catálogos, recetas, inventario y planificación desde la interfaz web. Antes de comenzar, [instala e inicia Friggy](getting-started.md).

## 1. Preparar los catálogos

Abre la navegación de catálogos y comprueba que existen las unidades y los tipos de comida iniciales. Después:

1. En **Ingredientes**, añade `Arroz`.
2. En **Etiquetas**, añade `Rápida` si quieres clasificar la receta.
3. Conserva una unidad y un tipo de comida existentes, por ejemplo `gramo` y `comida`.

Los catálogos son referencias compartidas. La aplicación puede impedir borrar un elemento que ya esté siendo utilizado.

## 2. Crear una receta

Ve a **Recetas** y selecciona **Crear receta**:

1. Usa `Arroz sencillo` como nombre y `25` minutos como tiempo estimado.
2. Añade una línea con `200` gramos de arroz.
3. Añade un paso, por ejemplo `Cocer el arroz hasta que esté tierno`.
4. Marca la línea de arroz dentro de **Ingredientes utilizados** del paso.
5. Selecciona la etiqueta `Rápida` y el tipo de comida `Comida`.
6. Guarda y revisa el detalle.

Las cantidades pertenecen a las líneas de la receta. Asociarlas a pasos indica dónde se usan, pero no vuelve a sumarlas para el inventario.

## 3. Registrar existencias

Ve a **Inventario** y añade un lote:

1. Selecciona `Arroz`.
2. Usa la misma unidad de la receta: `gramo`.
3. Introduce `500` como cantidad.
4. Elige una fecha de caducidad futura.

Friggy compara ingrediente y unidad de forma exacta. Un lote en kilogramos no cubre automáticamente una necesidad expresada en gramos.

## 4. Crear el plan semanal

Ve a **Planes semanales**:

1. Escribe un nombre y selecciona un lunes como fecha de inicio.
2. Crea el plan y abre su detalle.
3. En uno de los huecos de comida, selecciona `Arroz sencillo`.
4. Mantén una ración y, opcionalmente, define una hora prevista.

Puedes añadir, retirar y reordenar tipos de comida de cada día. Si defines una hora, Friggy muestra cuándo debería comenzar la preparación según el tiempo estimado de la receta.

## 5. Revisar necesidades

En **Necesidades de inventario** deberías ver:

* 200 gramos requeridos.
* 500 gramos disponibles.
* 0 gramos faltantes.

Solo cuentan los lotes con cantidad positiva cuya caducidad no haya pasado.

## 6. Completar la comida

Selecciona **Completar** en la asignación:

1. Indica que se consumen 200 gramos del lote registrado.
2. Confirma la operación.
3. Vuelve a **Inventario** y abre el lote.

La cantidad resultante será 300 gramos y el historial mostrará un movimiento de consumo. La comida queda completada y no puede consumirse una segunda vez.

Si una comida no se realiza, usa **Omitir esta comida** e indica un motivo. Una omisión no consume inventario y también cierra la asignación.

## Siguientes lecturas

* [Recetas y catálogos](recipes.md)
* [Planificación semanal](weekly-planning.md)
* [Inventario doméstico](inventory.md)
