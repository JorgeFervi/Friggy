# Fase 13 — Plantillas de planes diarios

- **Estado:** Implementada en código; cierre formal pendiente
- **Implementación observada:** 22 de agosto de 2026
- **Validación pendiente:** recorrido E2E/visual específico de plantillas y gate completo
- **Estimación:** 3–4 días
- **Dependencias:** [Fase 12](../completed/phases/12-daily-planning.md)
- **Guía ejecutable:** [Implementación de la fase 13](../implementation/13-daily-plan-templates-implementation.md)

## Resultado esperado

Permitir guardar configuraciones reutilizables de un día y aplicarlas a una o varias fechas elegidas, creando planes diarios independientes sin sobrescribir planificación existente.

La implementación actual incluye Domain, Application, migración PostgreSQL, repositorio, API, cliente y página Blazor. Existen pruebas de Domain, Application, endpoint y cliente HTTP, pero no el recorrido E2E de plantillas exigido por estos criterios; por ello la fase no se mueve todavía a `completed/`.

## Decisiones vinculantes

- `DailyPlanTemplate` es una raíz separada con nombre único normalizado y una colección ordenada de comidas.
- Una comida de plantilla puede contener tipo de comida, hora prevista y, opcionalmente, receta y comensales.
- La plantilla no tiene fecha ni estados completado/omitido; solo representa una intención futura.
- Aplicar una plantilla copia sus valores con identificadores nuevos. No se guarda un vínculo vivo y los cambios posteriores no alteran planes ya creados.
- La solicitud recibe fechas explícitas, sin duplicados, para admitir días contiguos o sueltos.
- La aplicación es atómica y conservadora: si alguna fecha ya tiene plan, devuelve `409 Conflict` y no modifica ningún día.

## Alcance

1. CRUD de plantillas y edición ordenada de sus comidas.
2. Validación de recetas y tipos de comida mediante referencias existentes.
3. Aplicación atómica a una colección de fechas.
4. Persistencia PostgreSQL con restricciones de nombre, tipo de comida y orden.
5. API `/api/daily-plan-templates` y cliente HTTP tipado.
6. Pantalla de plantillas y acción accesible para elegir fechas y aplicar.

## Fuera de alcance

- Recurrencias automáticas, reglas como “todos los lunes” o tareas programadas.
- Sobrescribir, fusionar o actualizar planes diarios existentes.
- Vinculación dinámica entre plantilla y planes creados.
- Estados de ejecución, consumo de inventario o lista de la compra dentro de la plantilla.

## Orden de ejecución

1. Escribir tests rojos de nombre, unicidad de tipo de comida, orden, raciones y copia sin identidad compartida.
2. Implementar el agregado y sus DTO/casos de uso con fakes manuales.
3. Añadir repositorio, configuración y migración con PostgreSQL real.
4. Implementar la aplicación multifecha con prevalidación completa y una sola unidad de trabajo.
5. Publicar API, cliente y componentes Blazor.
6. Cubrir CRUD y aplicación con Integration, bUnit, E2E y regresión visual.

## Criterios de aceptación

- El usuario puede crear, consultar, editar y borrar una plantilla no usada como entidad persistente independiente.
- Una plantilla conserva el orden, horarios, recetas opcionales y comensales configurados.
- Puede aplicarse a fechas contiguas o no contiguas, generando un `DailyPlan` diferente por fecha.
- Ningún plan comparte entidades hijas ni cambia cuando se edita la plantilla original.
- Si existe un plan en cualquiera de las fechas, no se crea ninguno y el usuario recibe las fechas en conflicto.
- Dos aplicaciones concurrentes no pueden crear planes duplicados.
- API, PostgreSQL, bUnit, E2E, accesibilidad y gate completo quedan verdes.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Sobrescribir planificación real | política inicial “crear solamente” y `409` con todas las fechas en conflicto |
| Copiar estados o identidades | factoría `DailyPlanTemplate.Instantiate` que solo copie configuración y cree nuevos IDs |
| Escritura parcial en muchas fechas | prevalidación y un único `SaveChangesAsync` transaccional |
| Plantilla inválida por referencias borradas | FK restrict y validación de referencias antes de guardar/aplicar |

## Handoff

La fase entrega una ayuda de creación; la lista de la compra continúa leyendo únicamente planes diarios materializados, nunca plantillas.
