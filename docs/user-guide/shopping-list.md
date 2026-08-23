# Lista de la compra

> **Estado:** vigente

La lista de la compra calcula qué falta para cubrir los planes de un intervalo inclusivo. Es una vista de solo lectura: no crea pedidos ni modifica inventario.

## Calcular la lista

Selecciona las fechas inicial y final. Friggy agrega las cantidades de las comidas planificadas que no estén omitidas, descuenta los lotes disponibles y muestra requerido, disponible y cantidad a comprar.

Las cantidades compatibles se convierten antes de compararse. Una receta puede usar cucharadas de aceite y el inventario litros; la lista expresará el resultado en mililitros o litros, nunca en una unidad configurada únicamente para cocinar.

## Límites

Masa, volumen y conteo son dimensiones independientes. Las unidades personalizadas marcadas como **Sin conversión** solo coinciden con esa misma unidad. La lista no redondea a botellas, paquetes ni otros formatos comerciales.
