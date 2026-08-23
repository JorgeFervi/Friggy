# Tu primera semana con Friggy

> **Estado:** vigente · **Duración aproximada:** 15 minutos

Este tutorial recorre catálogos, recetas, inventario y planificación desde la interfaz web. Antes de comenzar, [instala e inicia Friggy](getting-started.md).

## 1. Preparar los catálogos

Una instalación vacía incluye diez recetas de bienvenida, sus ingredientes, etiquetas, unidades y tipos de comida. Abre la navegación de catálogos y:

1. En **Ingredientes**, localiza `Arroz`.
2. En **Etiquetas**, localiza `Rápida`.
3. Conserva una unidad y un tipo de comida existentes, por ejemplo `gramo` y `comida`.

Puedes abrir cualquiera de las recetas incluidas para ver un ejemplo completo antes de crear la tuya. Si actualizas una instalación que ya tiene contenido, Friggy no mezcla automáticamente estos ejemplos con tus datos.

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
2. Selecciona `kilogramo`, compatible con los gramos de la receta.
3. Introduce `0,5` como cantidad.
4. Elige una fecha de caducidad futura.

Friggy convierte unidades de la misma dimensión. En este ejemplo, el lote de 0,5 kilogramos equivale a 500 gramos y puede cubrir la necesidad de la receta.

## 4. Crear planes diarios

Ve a **Planes diarios**:

1. Selecciona el intervalo que quieres consultar.
2. Pulsa **Planificar este día** en una fecha sin plan.
3. En uno de los huecos de comida, selecciona `Arroz sencillo`.
4. Mantén un comensal y, opcionalmente, define una hora prevista.

Puedes añadir, retirar y reordenar tipos de comida de ese día. Si defines una hora, Friggy muestra cuándo debería comenzar la preparación según el tiempo estimado de la receta.

## 5. Revisar necesidades

En **Necesidades de inventario** deberías ver:

* 200 gramos requeridos.
* 500 gramos disponibles.
* 0 gramos faltantes.

Solo cuentan los lotes con cantidad positiva cuya caducidad no haya pasado.

## 6. Completar la comida

Selecciona **Completar** en la asignación:

1. Indica que se consumen 0,2 kilogramos del lote registrado; Friggy valida que equivalen a los 200 gramos requeridos.
2. Confirma la operación.
3. Vuelve a **Inventario** y abre el lote.

La cantidad resultante será 0,3 kilogramos y el historial mostrará un movimiento de consumo de 0,2 kilogramos. La comida queda completada y no puede consumirse una segunda vez.

Si una comida no se realiza, usa **Omitir esta comida** e indica un motivo. Una omisión no consume inventario y también cierra la asignación.

## 7. Reutilizar el plan como plantilla

Ve a **Plantillas** y crea `Arroz semanal`:

1. Añade una comida de tipo `Comida` con la receta `Arroz sencillo`, una ración y la hora que prefieras.
2. Guarda la plantilla.
3. Selecciona dos fechas futuras que todavía no tengan plan.
4. Pulsa **Aplicar a fechas seleccionadas**.

Friggy creará dos planes independientes. Si alguna fecha ya estuviera planificada, no crearía ninguno y te mostraría el conflicto.

## 8. Calcular la lista de la compra

Abre **Lista de la compra** y selecciona un intervalo que incluya las dos fechas anteriores. Como ambas comidas siguen planificadas, la demanda total será de 400 gramos. Con los 0,3 kilogramos restantes en inventario, Friggy mostrará el equivalente a:

* 400 gramos requeridos.
* 300 gramos disponibles.
* 100 gramos faltantes.

Las comidas completadas u omitidas no forman parte de esta consulta. La lista se recalcula al cambiar planes o inventario y no guarda pedidos.

## Siguientes lecturas

* [Recetas y catálogos](recipes.md)
* [Planificación diaria](daily-planning.md)
* [Plantillas de planes diarios](daily-plan-templates.md)
* [Inventario doméstico](inventory.md)
