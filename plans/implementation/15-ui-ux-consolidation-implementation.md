# Implementación ejecutable — Fase 15

- **Fase relacionada:** [Fase 15 — Consolidación de UI y UX](../phases/15-ui-ux-consolidation.md)
- **Dependencia ejecutable:** fases 13 y 14 completadas, documentadas y con suites/baselines verdes
- **Skills aplicables:** `blazor`, `modern-csharp`, `xunit`, `playwright-visual-testing`, `run-tests`
- **Plataforma detectada:** .NET 10, Blazor Web App, xUnit v3 sobre Microsoft Testing Platform y regresión Pixelmatch existente

La implementación es una evolución exclusiva de `Friggy.Web`. No se modifican Domain, Application, Infrastructure, Api, contratos HTTP, persistencia ni migraciones. Cada defecto o comportamiento comienza con un test que falla por la razón esperada; después se implementa el cambio mínimo y se refactoriza manteniendo verdes las suites afectadas.

## Contratos transversales

| Área | Contrato de implementación |
|---|---|
| Estado | las páginas coordinan clientes HTTP; los hijos reciben parámetros y emiten callbacks |
| Formularios | `EditForm`, modelo dedicado, validación accesible y bloqueo de doble envío |
| Interactividad | preferir C#/Blazor; JS aislado solo para APIs del navegador como `<dialog>` |
| Cantidades | un formateador Web único, sin modificar valores ni DTO recibidos |
| Errores | mapear códigos/estado conocidos a texto útil; registrar detalle técnico sin mostrarlo directamente |
| Responsive | recomponer datos en móvil; no depender de overflow lateral del documento |
| Accesibilidad | WCAG 2.2 AA, teclado, foco, zoom 200 % y objetivos de 44 × 44 px |
| Visual | reutilizar la infraestructura xUnit/Pixelmatch actual; no añadir Playwright Test de Node |

## Matriz de recorridos y pruebas

| Recorrido | bUnit | E2E funcional | Visual | Accesibilidad |
|---|---:|---:|---:|---:|
| Navegación e Inicio | sí | sí | móvil/escritorio | teclado, landmarks, carga parcial |
| Lista y editor diario | sí | sí | vacío, datos, modal | foco, Escape, objetivos táctiles |
| Plantillas | sí | sí | vacío, edición, multifecha | orden, validación, confirmación |
| Lista de la compra | sí | sí | vacío, mixto, error | tabla/tarjetas equivalentes |
| Formularios largos | sí | sí | validación | cambio sin guardar y foco de error |

## 15.1 — Precondición y contrato visual

Antes de modificar componentes:

1. Ejecutar las suites de componentes, E2E y visuales de fases 13 y 14.
2. Confirmar que sus documentos reflejan el estado entregado y moverlos a `completed/` si corresponde.
3. Generar las capturas actuales sin aceptar cambios y revisar cualquier diferencia previa.
4. Inventariar el uso de variables CSS en `Friggy.Web` y contrastarlo con los tokens declarados.
5. Añadir un test focalizado que falle si reaparecen tokens no definidos en hojas propias.

Corregir como mínimo las referencias conocidas a `--radius-lg`, `--font-weight-semibold`, `--color-heading` y `--font-size-heading-2`, sustituyéndolas por tokens existentes. No crear alias solo para esconder una referencia accidental.

El primer commit funcional de la fase debe dejar un punto de partida verde y capturas coherentes; no debe mezclar todavía el rediseño de recorridos.

## 15.2 — Formularios, cantidades y errores

### Pruebas rojas

Extender `FriggyPrimitivesTests` y crear pruebas unitarias Web para demostrar:

- relación entre `label`, ayuda, error e `id` de un `FormField`;
- controles con altura mínima y estado deshabilitado reconocible;
- cantidades `1`, `1,25` y `1,001`, sin ceros finales ni redondeo oculto superior a tres decimales;
- traducción de errores conocidos y fallback seguro sin detalle técnico;
- advertencia solo cuando un formulario realmente modificado intenta navegar.

### Implementación mínima

Centralizar la representación de cantidades:

```csharp
internal static class QuantityFormatter
{
    public static string Format(decimal value) =>
        value.ToString("0.###", CultureInfo.GetCultureInfo("es-ES"));
}
```

Si las cantidades de un contrato admiten más de tres decimales, la prueba debe cerrar primero la política; no truncar datos en presentación de forma implícita.

Crear un traductor Web pequeño, por ejemplo `UserFacingError`, que reciba el error HTTP conocido y devuelva título, explicación y capacidad de reintento. El detalle técnico continúa en logging. Las páginas no renderizan `exception.Message`.

Adoptar `FormField` gradualmente en:

- editor y filtros de planes diarios;
- plantillas;
- lista de la compra;
- formularios de recetas que todavía dupliquen el patrón.

Usar `NavigationLock` o una abstracción Web equivalente en editores largos. La protección se desactiva después de guardar o cancelar y no intercepta navegación cuando no hubo cambios.

## 15.3 — Navegación e Inicio

### Pruebas rojas

- navegación con agrupación “Planificación”, iconos distinguibles y estado activo correcto;
- orden de tabulación estable tanto con menú expandido como colapsado;
- Inicio representa independientemente plan de hoy, caducidades, compra pendiente, acciones rápidas y recetas;
- si falla una consulta, las demás secciones siguen disponibles y la afectada permite reintentar;
- la fecha de “hoy” procede de `TimeProvider`, no de `DateTime.Now`.

### Implementación mínima

Conservar las rutas actuales y cambiar únicamente su organización visual. En móvil, la apertura/cierre del menú conserva foco y nombre accesible.

Inicio utiliza exclusivamente los clientes tipados existentes. Las cargas se coordinan desde la página con cancelación y estado independiente por sección; no se crea un store global. Los componentes de tarjeta reciben modelos de vista Web ya preparados y callbacks de navegación/reintento.

Orden recomendado del contenido:

1. saludo contextual y fecha;
2. plan de hoy con acción “Ver o completar plan”;
3. alertas próximas de inventario;
4. resumen de compra pendiente;
5. acciones rápidas;
6. recetas recientes o disponibles.

No bloquear toda la página detrás de una única animación de carga.

## 15.4 — Plan diario y resultado de comida

### Pruebas rojas

Extender `DailyPlanPageTests` y la suite del editor para cubrir:

- filas/tarjetas compactas de fecha y selección vigente;
- tarjeta de comida cerrada con hora, receta, raciones y estado resumidos;
- una sola tarjeta en edición y conservación de datos al cambiar de sección;
- guardado en curso, éxito y fallo mostrados junto a la tarjeta;
- doble envío bloqueado;
- resultado completado u omitido desde una acción explícita;
- apertura, foco inicial, recorrido Tab/Shift+Tab, Escape y restauración de foco del diálogo.

### Componentes

Firma orientativa del componente de comida:

```csharp
[Parameter, EditorRequired] public MealSlotViewModel Meal { get; set; } = default!;
[Parameter] public bool Expanded { get; set; }
[Parameter] public bool Busy { get; set; }
[Parameter] public EventCallback<Guid> EditRequested { get; set; }
[Parameter] public EventCallback<MealSlotChange> SaveRequested { get; set; }
[Parameter] public EventCallback<Guid> ResultRequested { get; set; }
```

`MealSlotCard` no llama a la API. La página carga el plan y coordina las mutaciones. El estado optimista solo se mantiene si existe una restauración determinista ante fallo; en caso contrario se conserva el valor confirmado y se muestra el progreso local.

`MealCompletionDialog` usa `<dialog>` nativo y el patrón JS aislado ya establecido por `ConfirmDialog`. Debe:

- enfocar el primer control útil;
- impedir que Tab salga del modal;
- cerrar con Escape sin aplicar cambios;
- deshabilitar confirmación durante el envío;
- devolver foco al elemento que lo abrió.

En 360 px, la pantalla inicial no presenta simultáneamente todos los selectores y motivos de omisión. El detalle se revela al editar o registrar resultado.

## 15.5 — Plantillas completas

Esta subfase cierra la experiencia de los contratos ya entregados por la Fase 13; no añade endpoints.

### Pruebas rojas

- crear y editar con el mismo formulario;
- añadir, quitar y reordenar comidas manteniendo orden y valores;
- validación por campo y resumen enfocado al primer error;
- resumen de plantilla legible sin abrir el editor;
- selección explícita de varias fechas y resultado por fecha;
- confirmación antes de eliminar y cancelación sin petición HTTP;
- cambios sin guardar al editar;
- fallo recuperable sin perder la edición local.

### Componentes y flujo

- `DailyPlanTemplateCard`: nombre, número de comidas, resumen ordenado y acciones.
- `DailyPlanTemplateForm`: modelo de edición propio, validación y callbacks de guardar/cancelar.
- `TemplateDateSelector`: selección multifecha accesible, resumen de fechas y eliminación individual.

La página transforma DTO a modelos de edición y vuelve a construir la petición al guardar. No muta la colección del DTO recibida. La aplicación multifecha debe mostrar qué fechas se aplicaron y cuáles fallaron si el contrato ya ofrece ese detalle; si la operación es atómica, el texto debe indicarlo sin simular resultados parciales.

## 15.6 — Comparaciones y lista de la compra

### Pruebas rojas

Crear pruebas de `QuantityComparison` y extender `ShoppingListPageTests` para comprobar:

- equivalencia de ingrediente, unidad, requerido, disponible y pendiente en ambas composiciones;
- tabla semántica visible en escritorio y tarjetas/definiciones legibles en móvil;
- ausencia de `min-width` que provoque overflow del documento a 360 px;
- filtro derivado “Pendientes/Todos” sin mutar ni persistir datos;
- cantidades con el formateador común;
- estados distintos para sin planificación, todo cubierto, rango inválido, carga y error;
- recalcular después de corregir fechas o reintentar un fallo.

Modelo exclusivo de presentación:

```csharp
internal sealed record QuantityComparisonItem(
    string Ingredient,
    string Unit,
    decimal Required,
    decimal Available,
    decimal Missing);
```

`QuantityComparison` recibe una colección y etiquetas de contexto. No conoce `ShoppingListResponse`, planes diarios ni clientes HTTP. La página adapta sus DTO a este modelo.

En escritorio se conserva una tabla con encabezados asociados. En móvil cada tarjeta repite las etiquetas para no depender de la posición de columna. Ambas representaciones contienen la misma información; CSS controla cuál participa en el layout según el breakpoint.

Reutilizar el componente en los requerimientos del detalle diario si el contrato visual coincide. Si aparece una diferencia funcional real, preferir dos componentes específicos que compartan formateo antes que parámetros condicionales crecientes.

## 15.7 — Contenido y accesibilidad

Realizar una revisión de texto y estados después de estabilizar los recorridos:

- usar “raciones” de forma consistente;
- reemplazar explicaciones del modelo interno por instrucciones de tarea;
- escribir vacíos que indiquen el siguiente paso;
- ofrecer “Reintentar” cuando repetir sea seguro;
- anunciar cargas y resultados asíncronos con regiones `aria-live` discretas;
- enfocar el resumen o primer campo inválido después de un envío fallido;
- mantener un único `h1`, jerarquía de encabezados y landmarks correctos.

Ampliar `ResponsiveAccessibilityAuditJourneyTests.PublicRoutes` con `/daily-plan-templates` y `/shopping-list`. La auditoría debe comprobar:

1. 360 × 800, 768 × 1024 y 1440 × 1000;
2. ausencia de overflow horizontal del documento;
3. nombres accesibles de controles y objetivos principales de al menos 44 × 44 px;
4. recorrido completo de teclado y foco visible;
5. diálogo modal y restauración de foco;
6. zoom del navegador al 200 % sin pérdida de acción o contenido.

El contraste se revisa contra WCAG 2.2 AA usando los tokens finales. No se incorpora una dependencia nueva solo para esta fase si la infraestructura actual permite una comprobación reproducible.

## 15.8 — E2E, visual y gate

### Recorridos E2E

1. Abrir Inicio con datos mixtos y navegar a cada acción principal.
2. Editar una comida, guardar, completar y comprobar el ciclo de foco del modal.
3. Crear una plantilla, reordenarla, editarla y aplicarla a varias fechas.
4. Cancelar y confirmar una eliminación de plantilla.
5. Calcular una compra con líneas cubiertas y pendientes, alternar el filtro y recalcular.
6. Repetir los recorridos críticos con viewport móvil y teclado.

Los escenarios crean sus propios datos y no dependen del orden ni de estado compartido.

### Matriz visual

| Página | Estado | Viewports |
|---|---|---|
| Inicio | datos completos y fallo parcial | 360, 1440 |
| Plan diario | vacío, datos y diálogo | 360, 1440 |
| Plantillas | vacío, edición y multifecha | 360, 1440 |
| Lista de la compra | vacío, comparación mixta y validación | 360, 1440 |

Congelar hora, animaciones y datos variables con los mecanismos existentes. Mantener el comparador Pixelmatch actual. Ante una diferencia:

1. abrir `actual`, `expected` y `diff`;
2. corregir si es una regresión;
3. documentar por qué el cambio es intencional;
4. solo entonces ejecutar:

```powershell
./scripts/update-visual-baselines.ps1 -Accept
```

No se utiliza la aceptación de baselines como método para obtener verde.

## 15.9 — Secuencia TDD y comandos

Los filtros corresponden a Microsoft Testing Platform en .NET 10 y se pasan sin separador `--`:

```powershell
dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.Shared.FriggyPrimitivesTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.DailyPlans.DailyPlanPageTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.DailyPlanTemplates.DailyPlanTemplatesPageTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.ShoppingLists.ShoppingListPageTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-class "Friggy.EndToEndTests.ResponsiveAccessibilityAuditJourneyTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-trait "Category=Visual"

./scripts/quality-gate.ps1
dotnet format Friggy.sln --verify-no-changes --no-restore
```

Los nombres de clases nuevas deben quedar exactamente alineados con estos filtros o actualizarse aquí antes del handoff. Durante cada subfase se ejecuta primero la clase focalizada, luego el proyecto afectado y finalmente el gate completo.

## 15.10 — Documentación y cierre

Actualizar después de implementar y verificar:

- `docs/product/current-capabilities.md` y `docs/product/roadmap.md`;
- las guías de usuario de Inicio, planificación, plantillas y lista de la compra;
- la documentación de componentes/tokens si cambia su contrato;
- la matriz de baselines y el recorrido de accesibilidad;
- `plans/README.md`, moviendo ambos documentos de fase 15 a `completed/`.

Checklist final:

- [ ] No se modificaron capas ni contratos fuera de `Friggy.Web` y sus pruebas/documentación.
- [ ] Tokens CSS válidos y controles principales de 44 × 44 px.
- [ ] Formularios accesibles, sin doble envío y con protección de cambios.
- [ ] Navegación e Inicio funcionan con carga parcial y teclado.
- [ ] Plan diario compacto y diálogo con ciclo de foco completo.
- [ ] Plantillas editables, reordenables, eliminables con confirmación y multifecha.
- [ ] Comparaciones coherentes y sin overflow en móvil.
- [ ] Cantidades, terminología, vacíos y errores unificados.
- [ ] Auditoría responsive, E2E, visual y zoom 200 % verdes.
- [ ] Baselines revisados antes de aceptar cambios.
- [ ] Documentación, formato y gate completo verdes.
