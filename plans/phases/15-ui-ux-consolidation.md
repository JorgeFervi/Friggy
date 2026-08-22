# Fase 15 — Consolidación de UI y UX

- **Estado:** Planificada
- **Estimación:** 10–15 días
- **Dependencias:** fases 13 y 14 implementadas, documentadas y con el gate verde
- **Guía ejecutable:** [Implementación de la fase 15](../implementation/15-ui-ux-consolidation-implementation.md)

## Resultado esperado

Consolidar la experiencia de Friggy para que los recorridos de uso frecuente sean claros, compactos y consistentes en móvil y escritorio, sin modificar el dominio ni los contratos HTTP. La fase aprovecha el sistema visual existente, completa la experiencia de plantillas y lista de la compra y convierte Inicio, navegación, formularios y estados de interacción en un conjunto coherente y accesible.

## Diagnóstico vinculante

La base visual de la Fase 11 es sólida: existen tokens, tipografías, primitivas compartidas, shell responsive, estilos de foco y regresión visual. La revisión del producto identifica una aplicación irregular de esa base:

- formularios recientes usan controles nativos y alturas inferiores al objetivo táctil en vez de las primitivas compartidas;
- el detalle del plan diario mantiene demasiados campos y acciones visibles simultáneamente en móvil;
- la lista de la compra conserva una tabla ancha que oculta información esencial en pantallas estrechas;
- el diálogo de finalización de comida no garantiza todavía el ciclo completo de foco de un diálogo modal;
- Inicio no resume el trabajo del día y la navegación presenta demasiadas opciones al mismo nivel;
- cantidades, términos, errores y estados vacíos no siguen una convención única;
- la cobertura responsive y visual no incluye todos los recorridos incorporados en las fases 13 y 14;
- existen referencias CSS a tokens no definidos y baselines que deben reconciliarse con la interfaz vigente.

Esta fase corrige esas diferencias. No sustituye el sistema visual ni introduce un framework de componentes nuevo.

## Principios de experiencia

1. **Prioridad a la tarea actual:** cada pantalla presenta primero la decisión o acción principal y revela el detalle cuando hace falta.
2. **Móvil sin pérdida de información:** el contenido se recompone; no depende de desplazar lateralmente una tabla para entender el resultado.
3. **Consistencia antes que variedad:** controles, estados, cantidades y textos reutilizan una sola convención.
4. **Respuesta visible:** cargar, guardar, completar, reintentar y fallar tienen estados próximos a la acción que los provoca.
5. **Accesibilidad por diseño:** teclado, foco, zoom, semántica, contraste y objetivos táctiles forman parte de la aceptación.
6. **Complejidad local:** los componentes coordinan presentación mediante parámetros y callbacks; las páginas conservan la orquestación HTTP.

## Decisiones vinculantes

- Todo el cambio funcional queda dentro de `Friggy.Web` y sus pruebas de componente/E2E.
- Se conservan rutas, endpoints, DTO, render mode y clientes HTTP tipados existentes.
- `Friggy.Web` no referencia Domain, Application, Infrastructure, `DbContext` ni repositorios.
- La mejora parte de `FormField`, `FriggyButton`, `ConfirmDialog` y los tokens existentes; no se añade una biblioteca de UI ni una CDN.
- Los diálogos modales usan `<dialog>` nativo, restauran foco y admiten teclado y Escape.
- Las cantidades se presentan con una utilidad Web única, cultura española y hasta tres decimales significativos de entrada, eliminando ceros finales.
- “Raciones” será el término visible común; los mensajes internos o excepciones sin filtrar no se muestran al usuario.
- La lista de la compra sigue siendo calculada y de solo lectura. El filtro visual “Pendientes/Todos” es derivado y no persiste marcas.
- El resumen de Inicio se compone con las consultas existentes. Un fallo parcial no oculta las demás secciones.
- Las capturas esperadas solo se aceptan después de revisar los archivos `actual` y `diff`.

## Arquitectura de presentación

La fase introduce componentes específicos, no un framework genérico:

- `MealSlotCard` para composición progresiva de cada comida del plan diario.
- `MealCompletionDialog` para el resultado de completar u omitir una comida.
- `QuantityComparison` y un modelo exclusivo de Web para mostrar requerido, disponible y pendiente como tabla en escritorio y tarjetas en móvil.
- `DailyPlanTemplateForm`, `DailyPlanTemplateCard` y `TemplateDateSelector` para creación, edición, orden y aplicación multifecha.
- una utilidad `QuantityFormatter` y un traductor de errores orientado a usuario.
- una protección de cambios sin guardar para formularios largos.

Los componentes de presentación reciben datos y emiten callbacks. Las páginas siguen siendo responsables de cargar, guardar y coordinar los clientes API.

## Alcance

### 1. Fundamentos visuales y de formularios

- eliminar referencias a tokens CSS inexistentes y usar exclusivamente el catálogo vigente;
- adoptar `FormField` y tamaños mínimos de 44 × 44 px en controles interactivos;
- asociar etiquetas, ayuda y validación con sus campos;
- unificar formato de cantidades, estados de carga y errores recuperables;
- avisar antes de abandonar formularios modificados sin guardar.

### 2. Navegación e Inicio

- agrupar plan diario, plantillas y lista de la compra bajo una categoría comprensible de planificación;
- asignar iconos distinguibles y conservar el acceso por teclado y las rutas actuales;
- convertir Inicio en un resumen de “hoy” con plan, próximas caducidades, compra pendiente, acciones rápidas y acceso a recetas;
- mantener operativas las secciones disponibles cuando otra consulta falle.

### 3. Plan diario

- hacer más compacto el selector/listado de fechas;
- mostrar cada comida como una tarjeta con resumen y edición progresiva;
- reducir la altura inicial en móvil y mantener visible una única tarea principal por tarjeta;
- presentar guardado y error junto al cambio afectado;
- sustituir campos permanentes de omisión por un flujo explícito de registro de resultado;
- completar el diálogo modal con foco inicial, confinamiento, Escape y restauración.

### 4. Plantillas

- usar un mismo formulario accesible para crear y editar;
- permitir editar, reordenar y resumir las comidas de una plantilla;
- seleccionar varias fechas de aplicación de forma explícita;
- confirmar la eliminación y describir con claridad reemplazos y resultados parciales;
- conservar las operaciones y contratos HTTP existentes.

### 5. Comparaciones de cantidades y lista de la compra

- presentar comparaciones como tarjetas en móvil y tabla semántica en escritorio;
- reutilizar la representación en requerimientos del plan y lista de la compra;
- mostrar cantidades compactas y símbolos de unidad de forma uniforme;
- ofrecer “Pendientes/Todos” como filtro de presentación;
- diferenciar resultado vacío, todo cubierto, validación, carga y error recuperable.

### 6. Contenido, accesibilidad y regresión visual

- sustituir mensajes técnicos por instrucciones orientadas a la tarea y acciones de reintento;
- comprobar teclado, foco visible, nombres accesibles, jerarquía, contraste y zoom al 200 %;
- incorporar plantillas y lista de la compra a la auditoría responsive;
- cubrir en visual móvil y escritorio estados vacío, con datos, validación, modal y error;
- reconciliar baselines heredados con el estado real antes de aceptar nuevos cambios.

## Matriz responsive mínima

| Contexto | Ancho de referencia | Comportamiento esperado |
|---|---:|---|
| Móvil compacto | 360 × 800 | una columna, tarjetas de comparación, acciones principales a ancho útil y sin overflow de documento |
| Tableta | 768 × 1024 | composición adaptable, navegación utilizable y densidad intermedia |
| Escritorio | 1440 × 1000 | tablas semánticas cuando aporten comparación y aprovechamiento equilibrado del espacio |
| Zoom | 200 % | contenido y acciones disponibles sin solapamiento ni pérdida funcional |

## Orden de ejecución

1. Estabilizar contratos visuales, inventariar tokens y obtener rojos focalizados.
2. Consolidar formularios, cantidades, errores y protección de cambios.
3. Mejorar navegación e Inicio con los clientes HTTP existentes.
4. Reestructurar la lista y el editor del plan diario y su diálogo.
5. Completar el recorrido de plantillas.
6. Unificar las comparaciones de cantidades y adaptar la lista de la compra.
7. Revisar contenido, accesibilidad, E2E y baselines.
8. Ejecutar el gate completo y actualizar documentación solo con resultados verificados.

## Criterios de aceptación

- Las rutas principales funcionan a 360 px sin overflow horizontal del documento ni información esencial inaccesible.
- Todos los controles primarios alcanzan 44 × 44 px y poseen nombre accesible y foco visible.
- Los formularios muestran validación junto al campo, impiden doble envío y protegen cambios sin guardar.
- Inicio permite comprender el plan del día, el inventario próximo y la compra pendiente sin entrar primero en cada sección.
- El plan diario reduce su densidad inicial en móvil y conserva edición, finalización y omisión completas.
- El diálogo de finalización se opera íntegramente con teclado, confina el foco y lo devuelve al disparador.
- Las plantillas pueden crearse, editarse, reordenarse, eliminarse con confirmación y aplicarse a varias fechas.
- La lista de la compra y los requerimientos muestran las mismas cantidades con una representación responsive coherente.
- Los errores conocidos tienen texto comprensible y reintento cuando corresponde; no se presenta `exception.Message` directamente.
- La auditoría responsive incluye todas las rutas públicas y los estados visuales relevantes tienen baseline móvil y escritorio revisado.
- No cambian Domain, Application, Infrastructure, Api, base de datos ni contratos HTTP.

## Fuera de alcance

- Dark mode, temas personalizables o rediseño de marca.
- Autenticación, cuentas, permisos o aislamiento multiusuario.
- Imágenes de recetas o ingredientes.
- Persistir, marcar, compartir, imprimir o exportar la lista de la compra.
- Conversiones entre unidades o cambios en el cálculo de requerimientos.
- Nuevos endpoints, DTO, migraciones o reglas de negocio.
- Aplicación móvil nativa o soporte offline.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Convertir una mejora visual en una reescritura | mantener rutas, contratos y primitivas; dividir por recorrido y test rojo |
| Ocultar funcionalidad al reducir densidad | probar todos los estados y hacer el detalle progresivo, no eliminarlo |
| Crear componentes genéricos difíciles de mantener | extraer solo patrones repetidos con una responsabilidad concreta |
| Aceptar regresiones como nuevos baselines | revisar `actual` y `diff` antes de ejecutar la aceptación |
| Dashboard con carga frágil | consultas independientes, estados por sección y cancelación |
| Divergencia entre móvil y escritorio | mismos datos y semántica; cambia únicamente la composición visual |

## Handoff

La fase concluye con una interfaz coherente y verificable sobre las capacidades ya entregadas. Las observaciones que requieran contratos, persistencia o reglas nuevas se registrarán como fases posteriores y no se resolverán ampliando silenciosamente este alcance.
