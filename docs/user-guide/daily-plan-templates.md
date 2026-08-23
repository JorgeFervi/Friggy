# Plantillas de planes diarios

> **Estado:** vigente

Las plantillas permiten guardar una configuración habitual de comidas y aplicarla a una o varias fechas sin repetir el formulario de cada día.

## Crear una plantilla

En **Plantillas**, indica un nombre y añade una o varias comidas. Para cada una puedes configurar:

* Tipo de comida.
* Receta opcional.
* Número positivo de raciones.
* Hora prevista opcional en formato `HH:mm`.
* Orden dentro del día.

Puedes reordenar o retirar comidas antes de guardar. Un tipo de comida solo puede aparecer una vez dentro de la misma plantilla.

## Aplicar una plantilla

Selecciona una o varias fechas y pulsa **Aplicar a fechas seleccionadas**. Friggy crea un plan diario independiente para cada fecha y copia únicamente la configuración; los estados de finalización u omisión no forman parte de la plantilla.

La aplicación es atómica y conservadora. Si ya existe un plan en cualquiera de las fechas seleccionadas, no se crea ninguno y la interfaz informa del conflicto. Una aplicación concurrente tampoco puede crear dos planes para el mismo día.

## Editar y borrar

Puedes editar el nombre y las comidas de una plantilla o eliminarla con confirmación. Los planes creados anteriormente no mantienen una relación viva con ella: editar o borrar la plantilla no modifica esos planes.

Continúa en [Planificación diaria](daily-planning.md) para asignar, omitir o completar las comidas materializadas.
