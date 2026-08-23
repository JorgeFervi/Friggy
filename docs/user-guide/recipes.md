# Recetas y catálogos

> **Estado:** vigente

## Catálogos compartidos

Antes de crear una receta puedes preparar:

* **Ingredientes:** los alimentos utilizados por recetas e inventario.
* **Unidades:** nombre, símbolo, dimensión y factor respecto a gramos, mililitros o unidades. También se indica si cada medida puede usarse al cocinar, al comprar o en ambos contextos.
* **Etiquetas:** clasificación libre, como vegetariana o rápida.
* **Tipos de comida:** desayuno, comida, cena u otros momentos configurables.

Los nombres deben ser válidos y no pueden duplicarse dentro de su catálogo. Si un elemento está referenciado, su eliminación puede rechazarse para proteger recetas, planes o lotes existentes.

## Crear una receta

Desde **Recetas**, selecciona **Crear receta** y completa:

1. Nombre y tiempo estimado total.
2. Una o varias líneas de ingrediente con ingrediente, unidad y cantidad positiva.
3. Uno o varios pasos con descripción y tiempo opcional.
4. Las líneas de ingredientes utilizadas en cada paso.
5. Etiquetas y tipos de comida recomendados.

Los botones **Subir** y **Bajar** conservan un orden explícito para ingredientes y pasos. Una misma referencia de ingrediente puede aparecer en varias líneas si necesita cantidades o unidades diferentes; las asociaciones de los pasos distinguen cada línea.

## Editar y eliminar

El detalle de una receta permite abrir su edición. Al retirar una línea de ingrediente, sus asociaciones con pasos también desaparecen. La cantidad sigue perteneciendo a la receta y no se duplica por estar asociada a varios pasos.

Una receta asignada a otros datos puede estar protegida frente al borrado. Si la API rechaza la operación, la interfaz muestra el conflicto sin eliminar referencias relacionadas.

## Relación con inventario y planificación

Las cantidades se multiplican por los comensales asignados en los planes diarios. Para que un lote cubra una necesidad debe coincidir el ingrediente y ser compatible la dimensión de la unidad. Por ejemplo, un lote en litros puede cubrir una receta expresada en mililitros o cucharadas; masa y volumen nunca se compensan.
