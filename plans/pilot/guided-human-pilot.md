# Guion de piloto humano guiado

- **Estado:** Preparado — sesión humana pendiente
- **Duración prevista:** 30–45 minutos
- **Producto:** MVP local monousuario

Este documento es un protocolo y no acredita que el piloto se haya realizado. La sesión solo podrá darse por completada cuando una persona ajena al código ejecute las tareas y se registren observaciones reales.

## Revisión previa del propietario

Antes de observar a otra persona, el propietario del producto debe dedicar una sesión de 60–90 minutos a un caso real y completar este registro:

| Comprobación | Resultado | Fricción o duda |
|---|---|---|
| Crear los ingredientes de una semana real | Pendiente | — |
| Crear una receta completa | Pendiente | — |
| Editar y eliminar datos | Pendiente | — |
| Planificar siete días | Pendiente | — |
| Reiniciar y comprobar persistencia | Pendiente | — |
| Recorrer técnicamente la creación de receta | Pendiente | — |
| Explicar la responsabilidad de cada capa | Pendiente | — |

El [recorrido técnico de creación de recetas](../../docs/development/recipe-creation-walkthrough.md) sirve como guía. Esta revisión debe hacerse antes de modificar el código para evitar que una preferencia técnica se confunda con una necesidad del producto.

## Preparación

- Confirmar que el árbol de trabajo está limpio y que `scripts/quality-gate.ps1` termina en verde.
- Ejecutar `scripts/setup.ps1` y `scripts/start.ps1` sin eliminar el volumen existente.
- Usar un conjunto de datos vacío o identificado expresamente para el piloto.
- Elegir una persona que no haya participado en la implementación.
- No registrar nombre, correo ni otros datos personales; basta con describir su familiaridad con aplicaciones similares.

## Datos de la sesión

| Campo | Valor |
|---|---|
| Fecha y hora | Pendiente |
| Perfil general del participante | Pendiente |
| Versión o commit | Pendiente |
| Entorno | Pendiente |
| Observador | Pendiente |

## Instrucciones para el participante

Explicar únicamente que Friggy sirve para registrar recetas y planificar una semana. Entregar estos objetivos sin indicar botones ni rutas:

1. Crear los ingredientes necesarios para una receta conocida.
2. Registrar la receta con cantidades, unidades y al menos dos pasos.
3. Crear una planificación semanal.
4. Asignar la receta a una comida concreta.
5. Introducir un dato no válido, interpretar el mensaje y corregirlo.
6. Cerrar la aplicación, volver a abrirla y comprobar que la asignación permanece.

El observador no ayudará salvo que la persona quede bloqueada. Cada intervención se contabiliza como ayuda.

## Registro de tareas

| Tarea | Completada | Tiempo aproximado | Ayudas | Dudas, errores o frases literales |
|---|---|---:|---:|---|
| Crear ingredientes | Pendiente | — | — | — |
| Crear receta | Pendiente | — | — | — |
| Crear semana | Pendiente | — | — | — |
| Asignar receta | Pendiente | — | — | — |
| Corregir dato inválido | Pendiente | — | — | — |
| Comprobar persistencia | Pendiente | — | — | — |

## Hallazgos

| Evidencia observada | Clasificación | Severidad | Decisión | Test de regresión |
|---|---|---|---|---|
| Pendiente | — | — | — | — |

Clasificaciones admitidas:

- **Defecto bloqueante:** impide completar el recorrido, pierde datos o deja la aplicación sin recuperación razonable.
- **Defecto menor:** comportamiento incorrecto con alternativa clara y sin pérdida de datos.
- **Mejora de experiencia:** el comportamiento es correcto, pero el esfuerzo, lenguaje o respuesta visible generan fricción.
- **Funcionalidad posterior:** petición que amplía el alcance del MVP.

No se clasificará una expectativa como defecto si el comportamiento actual coincide con el alcance documentado.

## Cierre de la sesión

1. Reproducir cada defecto confirmado con un test rojo en la capa más baja capaz de demostrarlo.
2. Aplicar la corrección mínima y ejecutar primero la suite afectada.
3. Incorporar mejoras de experiencia pequeñas únicamente cuando exista evidencia registrada.
4. Mantener las funcionalidades nuevas en el backlog posterior al MVP.
5. Ejecutar `scripts/quality-gate.ps1` después de las correcciones.

El piloto habilita la planificación de la Fase 7 solo si no quedan defectos bloqueantes, el gate está verde y el [registro de decisiones de inventario](../discovery/07-inventory-decisions.md) está confirmado.
