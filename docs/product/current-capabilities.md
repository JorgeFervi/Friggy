# Funcionalidades actuales

> **Estado:** vigente · **Última revisión:** 23 de agosto de 2026

Friggy es una aplicación web local y monousuario para organizar recetas, planes diarios e inventario doméstico. Web y API se ejecutan como procesos separados y los datos se conservan en PostgreSQL.

## Catálogos

La aplicación permite crear, consultar, editar y eliminar:

* Ingredientes.
* Tipos de unidad con nombre, símbolo, dimensión, factor de conversión y usos permitidos.
* Etiquetas de receta.
* Tipos de comida con un orden configurable.

La base de datos incluye inicialmente unidades de masa, volumen y conteo. Cada unidad puede habilitarse para cocina, compra o ambos contextos. Un elemento referenciado por recetas, planes o inventario puede estar protegido frente al borrado; su dimensión y factor tampoco se reinterpretan mientras esté en uso.

## Recetas

Cada receta puede incluir:

* Nombre y tiempo estimado total.
* Ingredientes con cantidad, unidad y orden.
* Pasos con descripción, tiempo opcional y orden.
* Asociación de una o varias líneas de ingredientes con cada paso.
* Etiquetas y tipos de comida recomendados.

Las recetas se pueden crear, editar, consultar y eliminar desde la interfaz.

## Planificación diaria

Cada plan corresponde a una fecha y solo puede existir uno por día. Se puede consultar y planificar cualquier intervalo sin exigir semanas completas. En cada plan se puede:

* Añadir, retirar y reordenar huecos de comida.
* Asignar o retirar una receta.
* Definir un número entero positivo de raciones.
* Establecer una hora prevista y consultar el inicio de preparación derivado del tiempo de la receta.
* Omitir una asignación indicando un motivo y una alternativa opcional.
* Completar una comida seleccionando cantidades de lotes compatibles.
* Eliminar un plan mientras no contenga comidas completadas.

Las comidas omitidas no consumen inventario. Una comida completada queda cerrada y no se vuelve a consumir si se repite la petición.

## Inventario

Las existencias se registran por lotes independientes con ingrediente, unidad de compra, cantidad y caducidad. Cada lote conserva un historial inmutable de:

* Alta inicial.
* Consumo.
* Ajuste a cantidad real.
* Descarte.

También se puede corregir su caducidad. La lista oculta por defecto los lotes agotados o caducados, que pueden mostrarse mediante el filtro correspondiente.

## Necesidades y finalización

El detalle de un plan calcula las cantidades requeridas, disponibles y faltantes por ingrediente y dimensión. Convierte masa, volumen y conteo mediante sus factores base, mantiene separadas las dimensiones incompatibles y excluye lotes caducados o agotados.

Al completar una comida, el usuario elige cuánto consumir de cada lote compatible aunque la receta use otra unidad de la misma dimensión. La operación registra el movimiento en la unidad original del lote y cierra la asignación de forma transaccional. Si no hay existencias suficientes, Friggy informa de la necesidad restante en una unidad de compra.

## Lista de la compra

La lista de la compra es una consulta de solo lectura para un intervalo inclusivo. Agrega la demanda de los planes no omitidos, descuenta el inventario utilizable mediante conversiones compatibles y expresa requerido, disponible y faltante en una unidad habilitada para compra. Las unidades exclusivamente culinarias, como cucharadas, no aparecen como propuesta de compra.

## Límites actuales

La versión actual no incluye:

* Cuentas, autenticación ni separación de datos por usuario.
* Despliegue público o configuración de producción.
* Inteligencia artificial.
* Imágenes de recetas o ingredientes.
* Información nutricional.
* Conversión entre masa y volumen mediante densidad.
* Presentaciones comerciales, tamaños de envase o redondeo a paquetes.

Consulta la [hoja de ruta](roadmap.md) para distinguir capacidades implementadas de posibles evoluciones.
