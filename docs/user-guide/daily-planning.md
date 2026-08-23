# Planificación diaria

> **Estado:** vigente

## Crear y consultar planes

Cada plan pertenece a una única fecha y como máximo puede existir uno por día. En **Planes diarios** puedes consultar cualquier intervalo inclusivo; los días sin plan se muestran explícitamente y se crean de forma individual.

Desde el resultado del intervalo también puedes borrar un plan. Friggy solicita confirmación y bloquea el borrado si alguna de sus comidas ya se ha completado, para conservar la trazabilidad del inventario.

Al abrir un día puedes añadir, retirar y reordenar huecos, asignar recetas, indicar comensales y definir una hora prevista. Si hay receta y hora, Friggy calcula el inicio de preparación restando su tiempo estimado.

## Necesidades y estados

Las necesidades multiplican cada ingrediente por los comensales y agrupan las cantidades compatibles por ingrediente y dimensión. Friggy convierte masa con masa, volumen con volumen y conteo con conteo; las unidades marcadas como **Sin conversión** solo coinciden consigo mismas. Se excluyen comidas omitidas y lotes agotados o ya caducados.

Una comida se puede omitir con motivo o completar seleccionando consumos de lotes compatibles. La finalización es transaccional e idempotente; una comida completada queda bloqueada.

Si repites con frecuencia una configuración, consulta [Plantillas de planes diarios](daily-plan-templates.md).
