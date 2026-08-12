# Decisiones previas — Inventario doméstico

- **Estado:** Decisiones funcionales confirmadas — incorporadas al plan de la Fase 8
- **Efecto:** El análisis de requisitos queda cerrado; la implementación de inventario no comenzará hasta completar el gate de este documento

El primer incremento incorporará el inventario como una capacidad nueva dentro de las capas actuales. Permitirá registrar existencias por lotes, conocer su caducidad, conservar el historial de cambios y comparar las cantidades disponibles con las necesidades de una planificación semanal.

## Decisiones confirmadas

| Decisión | Resolución | Motivo |
|---|---|---|
| Ración base de una receta | Las cantidades de una receta representan una ración para una persona | Proporciona una base única y predecible para calcular necesidades |
| Raciones planificadas | Cada asignación de receta en el plan semanal indicará un número entero positivo de raciones; una asignación nueva comienza con una ración | Permite adaptar una misma receta al número de personas sin duplicarla |
| Representación del stock | Las existencias se representan mediante lotes con ingrediente, cantidad actual, unidad y fecha de caducidad | Dos compras del mismo ingrediente pueden tener cantidades y caducidades diferentes |
| Caducidad y disponibilidad | Un lote caducado no forma parte del stock utilizable; la fecha de caducidad es válida durante todo ese día y se permiten fechas pasadas | Permite registrar y consultar existencias caducadas sin utilizarlas para cubrir necesidades |
| Historial | Cada cambio de cantidad conserva un movimiento asociado al lote | Permite conocer por qué cambió el inventario sin introducir event sourcing ni CQRS |
| Operaciones | Se mantienen añadir, consumir, ajustar y descartar | Distingue el alta, el uso, la corrección y la retirada de existencias |
| Corrección del historial | Los movimientos no se editan ni se eliminan; un error se corrige mediante un movimiento de ajuste posterior | Preserva la trazabilidad y evita reescribir lo ocurrido |
| Selección para el consumo | El usuario elige explícitamente el lote; los lotes se muestran primero por caducidad más próxima | Facilita consumir antes lo que caduca sin automatizar la elección |
| Consumo parcial | Si el lote seleccionado contiene menos de lo solicitado, se consume toda su cantidad disponible y se informa de la cantidad restante | Evita stock negativo sin bloquear el registro del consumo real |
| Lote agotado | El lote se conserva con cantidad cero para mantener su historial y se oculta por defecto del inventario disponible | Mantiene la trazabilidad sin saturar la vista habitual |
| Edición del lote | La caducidad puede corregirse; ingrediente y unidad no pueden cambiar después de que exista un movimiento posterior al alta | Permite corregir datos sin reinterpretar movimientos ya registrados |
| Alcance del cálculo | Se calcula el total del plan semanal seleccionado, multiplicando las cantidades de cada receta por las raciones asignadas | Ofrece una visión completa de necesidades y carencias de la semana |
| Existencias parciales | Se muestran las cantidades requerida, disponible y faltante por cada combinación de ingrediente y unidad | Hace visible qué parte de la necesidad está cubierta |
| Finalización de una comida | Planificar o modificar una asignación no descuenta existencias; el consumo solo se registra al completar la comida | Una planificación no demuestra que la receta se haya preparado |
| Reversión de una comida completada | Una comida completada no se reabre automáticamente en el primer incremento; cualquier corrección del inventario se registra mediante ajustes posteriores y el estado completado queda disponible para consulta | Evita consumos inversos implícitos y mantiene la trazabilidad de los movimientos |
| Semántica del ajuste | El usuario indica la cantidad real contada y la aplicación calcula y registra la diferencia respecto a la cantidad anterior | Representa directamente el resultado de un recuento y reduce errores de signo |
| Descarte superior a las existencias | Se descarta la cantidad disponible, el lote queda agotado y se informa de la parte que no pudo registrarse | Conserva el stock no negativo y refleja toda la cantidad que realmente podía retirarse |
| Catálogos referenciados | No se pueden borrar ingredientes ni unidades referenciados por lotes o movimientos; solo se permite cambiar su nombre o símbolo | Evita romper el inventario y conserva la legibilidad del historial |
| Conversiones | No se realizan conversiones, equivalencias ni inferencias entre unidades | Evita factores discutibles, densidades implícitas y cálculos difíciles de verificar |
| Correspondencia de cantidades | Una existencia solo cubre una necesidad cuando coinciden exactamente el ingrediente y la unidad | Mantiene el cálculo determinista sin reintroducir conversiones |
| Unidades diferentes | Las cantidades del mismo ingrediente expresadas en unidades distintas se calculan por separado; no existe un estado especial de unidad incompatible | Una unidad diferente no se convierte, no se agrega y no requiere reglas adicionales |
| Ubicaciones | No se modelan frigorífico, congelador, despensa ni otras ubicaciones | No son necesarias para conocer cantidades, caducidades y carencias |
| Arquitectura | Se conservan los proyectos y la dirección de dependencias actuales | Inventario no será un microservicio, un proyecto adicional ni una excusa para introducir CQRS |

## Flujo de finalización de una comida

1. El usuario marca como completada una comida asignada en el plan semanal.
2. La aplicación calcula las cantidades requeridas multiplicando la receta por las raciones asignadas.
3. Para cada combinación de ingrediente y unidad, la aplicación muestra los lotes no caducados y con cantidad disponible, ordenados por fecha de caducidad ascendente.
4. El usuario selecciona explícitamente uno o varios lotes de los que se consumirá cantidad.
5. Cada consumo afecta a un único lote y crea su propio movimiento.
6. Cuando las existencias seleccionadas no cubren la necesidad, la aplicación registra el consumo disponible y muestra la cantidad restante sin producir stock negativo.
7. Completar la comida y crear sus movimientos de consumo forman una única operación atómica, para impedir una comida completada sin los movimientos elegidos o movimientos duplicados por el mismo cierre.

## Fuera del primer incremento

- Ubicaciones físicas del inventario.
- Conversiones, densidades y equivalencias entre unidades.
- Tratamiento específico de unidades incompatibles.
- Lista de la compra.
- Inteligencia artificial.
- Autenticación, varios usuarios y despliegue público.

## Reglas derivadas

- Las raciones de una asignación semanal deben ser números enteros mayores que cero.
- Las necesidades del plan se obtienen multiplicando cada cantidad de receta por las raciones de su asignación.
- Las líneas repetidas con el mismo ingrediente y unidad se agregan antes de compararlas con el inventario.
- Los lotes caducados continúan visibles para consulta e historial, pero no cubren necesidades ni se ofrecen para consumo.
- Los lotes distintos no se fusionan automáticamente aunque compartan ingrediente, unidad y caducidad.
- La cantidad de un lote nunca puede ser negativa.
- Ajustar es la única operación que puede aumentar o disminuir la cantidad de un lote existente; consumir y descartar solo pueden reducirla.
- Un ajuste recibe la cantidad real contada, no una diferencia introducida manualmente.
- Consumir o descartar más de lo disponible reduce el lote a cero e informa de la cantidad no registrada.
- El lote conserva su cantidad actual y los movimientos explican sus cambios; conservar historial no convierte el inventario en un sistema de event sourcing.
- Cualquier operación que cambie cantidad y cree movimientos debe completarse de forma atómica.
- Una comida completada no puede volver a generar consumos por una repetición de la misma solicitud.
- Una comida completada permanece cerrada en el primer incremento; las correcciones posteriores afectan al inventario mediante ajustes, no al estado de la comida.

## Gate para abrir la Fase 8

- La validación manual previa se considera cerrada expresamente por el propietario del producto y no quedan defectos bloqueantes abiertos.
- Los límites de la primera entrega permanecen sin lista de la compra, conversiones, ubicaciones, IA, autenticación ni despliegue público.
- Los contratos, la migración y los escenarios de integración con PostgreSQL están identificados en el [plan completado de la Fase 8](../completed/phases/08-inventory-and-meal-completion.md) y su [guía ejecutada](../completed/implementation/08-inventory-and-meal-completion-implementation.md).
- `scripts/quality-gate.ps1` está en verde sobre el commit de partida.

La implementación deberá seguir TDD y mantener `Domain <- Application <- Infrastructure <- Api`. `Friggy.Web` consumirá los nuevos contratos exclusivamente por HTTP.
