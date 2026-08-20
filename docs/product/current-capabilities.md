# Funcionalidades actuales

> **Estado:** vigente · **Última revisión:** 20 de agosto de 2026

Friggy es una aplicación web local y monousuario para organizar recetas, planes semanales e inventario doméstico. Web y API se ejecutan como procesos separados y los datos se conservan en PostgreSQL.

## Catálogos

La aplicación permite crear, consultar, editar y eliminar:

* Ingredientes.
* Tipos de unidad con nombre y símbolo.
* Etiquetas de receta.
* Tipos de comida con un orden configurable.

La base de datos incluye inicialmente unidades de uso común y los tipos desayuno, comida y cena. Un elemento referenciado por recetas, planes o inventario puede estar protegido frente al borrado.

## Recetas

Cada receta puede incluir:

* Nombre y tiempo estimado total.
* Ingredientes con cantidad, unidad y orden.
* Pasos con descripción, tiempo opcional y orden.
* Asociación de una o varias líneas de ingredientes con cada paso.
* Etiquetas y tipos de comida recomendados.

Las recetas se pueden crear, editar, consultar y eliminar desde la interfaz.

## Planificación semanal

Un plan comienza en lunes, abarca siete días y contiene nombre y descripción opcional. En cada día se puede:

* Añadir, retirar y reordenar huecos de comida.
* Asignar o retirar una receta.
* Definir un número entero positivo de raciones.
* Establecer una hora prevista y consultar el inicio de preparación derivado del tiempo de la receta.
* Omitir una asignación indicando un motivo y una alternativa opcional.
* Completar una comida seleccionando cantidades de lotes compatibles.

Las comidas omitidas no consumen inventario. Una comida completada queda cerrada y no se vuelve a consumir si se repite la petición.

## Inventario

Las existencias se registran por lotes independientes con ingrediente, unidad, cantidad y caducidad. Cada lote conserva un historial inmutable de:

* Alta inicial.
* Consumo.
* Ajuste a cantidad real.
* Descarte.

También se puede corregir su caducidad. La lista oculta por defecto los lotes agotados o caducados, que pueden mostrarse mediante el filtro correspondiente.

## Necesidades y finalización

El detalle de un plan calcula las cantidades requeridas, disponibles y faltantes para cada combinación exacta de ingrediente y unidad. No realiza conversiones entre unidades y excluye lotes caducados o agotados.

Al completar una comida, el usuario elige cuánto consumir de cada lote compatible. La operación registra los movimientos y cierra la asignación de forma transaccional. Si no hay existencias suficientes, Friggy puede informar de la necesidad restante; no genera todavía una lista de la compra.

## Límites actuales

La versión actual no incluye:

* Cuentas, autenticación ni separación de datos por usuario.
* Despliegue público o configuración de producción.
* Inteligencia artificial.
* Imágenes de recetas o ingredientes.
* Información nutricional.
* Conversiones automáticas entre unidades.
* Lista de la compra.

Consulta la [hoja de ruta](roadmap.md) para distinguir capacidades implementadas de posibles evoluciones.
