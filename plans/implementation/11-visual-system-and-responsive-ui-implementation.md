# Implementación ejecutable — Fase 11

- **Fase relacionada:** [Fase 11 — Sistema visual y experiencia responsive](../phases/11-visual-system-and-responsive-ui.md)
- **Skills aplicables:** `blazor`, `modern-csharp`, `xunit`, `playwright-visual-testing`, `quality-ci`

La fase es una migración de presentación. No modifica Domain, Application, Infrastructure ni Api. Todo comportamiento de componentes comienza con un test bUnit/xUnit v3 fallido; los baseline iniciales se generan únicamente después de que la semántica y el diseño de cada incremento estén aprobados.

> **Progreso:** 11.1 completada. La siguiente unidad ejecutable es **11.2 — Primitivos y estados comunes**.

## Reglas transversales

1. Conservar rutas, `data-testid` necesarios, clientes HTTP y mensajes funcionales existentes.
2. Mantener lógica de carga, cancelación y mutación en las páginas; los componentes visuales reciben datos y emiten `EventCallback`.
3. No mezclar valores hexadecimales, fuentes o sombras fuera de los tokens, salvo recursos SVG autocontenidos.
4. No ocultar contenido para resolver responsive: cambiar composición preservando orden de lectura y acciones.
5. Ejecutar después de cada subfase el test focalizado, la suite Component y los E2E afectados; actualizar baseline solo si el cambio esperado fue revisado.

## 11.1 — Contrato visual y banco de regresión — Completada

### Rojo

1. Añadir tests de contrato para idioma `es`, carga de hojas locales y ausencia de CDN o fuentes remotas.
2. Añadir tests del helper visual para baseline ausente, dimensiones distintas, diferencia bajo límite y diferencia sobre límite.
3. Añadir un E2E visual mínimo que capture un fixture estable en 360, 768 y 1440 px y falle mientras no exista la infraestructura de comparación.

### Verde mínimo

1. Incorporar Inter 400/500/600/700 y Fraunces 600 en WOFF2 junto con sus licencias.
2. Crear `fonts.css`, `tokens.css`, `base.css` y el punto de entrada de estilos; declarar la paleta, escalas y `font-display: swap`.
3. Corregir `<html lang="es">`, normalización de `box-sizing`, fondo, tipografía, foco global y reduced motion.
4. Añadir `package.json` y `pnpm-lock.yaml` solo con `pixelmatch` y `pngjs`; `setup.ps1` comprueba Node LTS y ejecuta `pnpm install --frozen-lockfile`.
5. Crear el comparador invocado por los tests xUnit: baseline versionado por plataforma, actual/diff en `TestResults/visual`, fallo por ausencia o dimensiones y ratio máximo 0,002.
6. Añadir un comando explícito `scripts/update-visual-baselines.ps1 -Accept` que nunca sea llamado por el gate.

### Refactor y aceptación

- El helper fija Chromium, locale, zona horaria, light scheme, device scale factor 1 y desactiva animaciones/caret.
- `.gitignore` excluye actual y diff pero no baseline.
- Documentar que los baseline se generan y comparan en el entorno Windows/Chromium validado; una futura CI deberá reproducir ese entorno o mantener baseline por plataforma.

Se observaron cinco rojos focalizados por idioma, hojas, fuentes, tokens, lockfile y comparador ausentes. El verde incorpora Inter y Fraunces desde Fontsource 5.3.0 con licencias OFL, recursos WOFF2 locales, idioma español, paleta/tokens, base accesible y reduced motion. Pixelmatch 7.2.0 y pngjs 7.0.0 quedan fijados con pnpm exclusivamente para test; el comparador supera ausencia de baseline, dimensiones incompatibles y diferencias dentro/fuera del umbral.

Playwright .NET captura Inicio en 360×800, 768×1024 y 1440×1000. Las tres imágenes se revisaron antes de aceptar los baseline Windows/Chromium; un foco visual incorrecto en el `h1` programático se corrigió antes de consolidarlos. `scripts/update-visual-baselines.ps1` exige `-Accept`, mientras setup y quality gate restauran y auditan el lockfile sin aceptar imágenes automáticamente.

El gate final completó build Release con 0 warnings, formato limpio, auditorías pnpm/NuGet sin vulnerabilidades y 369/369 pruebas: 115 Domain, 71 Application, 85 Integration, 80 Component y 18 E2E.

## 11.2 — Primitivos y estados comunes

### Rojo

1. bUnit verifica variantes, disabled/busy, tipo de botón y propagación única de eventos en `UiButton`.
2. Verificar nombre accesible de `UiIconButton`, semántica y contenido de `UiCard`, `PageHeader`, `StatusBadge` y `FeedbackPanel`.
3. Verificar asociación label–control, ayuda y error en `FormField` sin duplicar identificadores.
4. Verificar que `ConfirmDialog` abre, mueve foco, confirma/cancela una sola vez y restaura foco.

### Verde mínimo

1. Implementar enums cerrados para variantes y tonos; no aceptar clases arbitrarias para alterar semántica.
2. Crear iconos SVG locales para navegación y acciones ya existentes: inicio, receta, calendario, inventario, catálogos, menú, cerrar, añadir, editar, borrar, volver y tiempo.
3. Implementar componentes con CSS isolation, parámetros pequeños y `RenderFragment` solo donde haya composición real.
4. Sustituir progresivamente confirmaciones `IJSRuntime confirm` al migrar cada página, no mediante un cambio masivo previo.

### Refactor y aceptación

- Galería bUnit cubre cada variante y estado; no se añade una ruta pública de showcase.
- Targets táctiles alcanzan 44×44 px y foco/contraste cumplen AA.

## 11.3 — Shell, sidebar y drawer

### Rojo

1. Actualizar `MainLayoutTests` para navegación agrupada, enlace de salto, estado activo y botón del drawer.
2. bUnit cubre abrir/cerrar, overlay, `Escape`, selección de enlace y atributos ARIA.
3. Playwright cubre sidebar visible a 1440 px, drawer a 768 y 360 px, bloqueo de scroll y retorno de foco.

### Verde mínimo

1. Separar `AppShell`, `SidebarNavigation`, `MobileHeader` y `NavigationDrawer` sin cambiar `MainLayout` como layout raíz.
2. Mostrar Inicio, Recetas, Planes e Inventario como navegación principal; agrupar los cuatro catálogos en una sección claramente etiquetada.
3. Mantener sidebar fijo desde 1200 px y drawer por debajo; el contenido nunca queda debajo de la navegación.
4. Usar un módulo JS local mínimo solo para focus trap, restauración de foco y bloqueo de scroll; no incluir librerías.
5. Adaptar error de Blazor y `ReconnectModal` al idioma y tokens sin romper su script de reconexión.

### Refactor y aceptación

- Capturas aprobadas del shell vacío y con contenido en los tres viewports.
- Tabulación y lector de pantalla recorren navegación y contenido en orden lógico.

## 11.4 — Inicio y recetas

### Rojo

1. `HomeComponentTests` exige cargar recetas por `IRecipesApiClient` y renderizar loading, error, vacío y cards.
2. bUnit cubre `RecipeCard` y `RecipeGrid`: nombre, minutos, enlace de detalle, editar y solicitud de borrado según contexto.
3. Actualizar pruebas de `/recipes`, creación, edición y detalle para el nuevo sistema sin rebajar sus aserciones funcionales.
4. E2E verifica grid de una columna a 360 px, dos o más columnas según ancho, navegación por card y acciones accesibles.

### Verde mínimo

1. Convertir Inicio en una portada breve con título, texto de apoyo, acción “Crear receta” y galería obtenida del endpoint actual.
2. Estructurar Inicio con `PageSection` independientes; no representar estadísticas ni placeholders vacíos.
3. Reutilizar `RecipeGrid` en `/recipes`; mantener la confirmación, estados pending y reglas de borrado.
4. Migrar formulario, filas de ingredientes/pasos y detalle a cards/secciones, preservando orden, labels y asociaciones existentes.
5. En móvil apilar controles y acciones; en escritorio usar columnas solo cuando no alteren el orden de lectura.

### Refactor y aceptación

- Baseline de Inicio vacío y con recetas, formulario y detalle en 360 y 1440 px.
- No se amplía `RecipeListItemResponse`; las cards muestran exclusivamente nombre, tiempo y acciones reales.

## 11.5 — Planificación semanal

### Rojo

1. Actualizar bUnit de lista, formulario y detalle para `PageHeader`, feedback, cards móviles y acciones existentes.
2. Añadir pruebas de `WeeklyPlanCalendar` en composición compacta y amplia sin alterar selección, reordenación, horarios, omisión o finalización.
3. Playwright verifica navegación, foco y ausencia de overflow con contenido largo a 360, 768 y 1440 px.

### Verde mínimo

1. Migrar listado y formulario de planes; cards en móvil y tabla/grid en tablet/escritorio.
2. Rediseñar el calendario con días como superficies, jerarquía clara de comidas y acciones agrupadas.
3. Mantener controles nativos y flujos de completion/skip; aplicar badges a estados ya existentes.
4. Adaptar diálogos y selección de lotes a móvil sin convertirlos en nuevas reglas de negocio.

### Refactor y aceptación

- Baseline de lista, calendario poblado y finalización en móvil y escritorio; tablet cubre el cambio de layout.
- Todas las pruebas de planificación e inventario continúan verdes.

## 11.6 — Inventario

### Rojo

1. bUnit cubre formulario, filtro, feedback y colección responsive sin perder agotados/caducados.
2. Mantener aserciones de consumir, descartar, ajustar y corregir caducidad en detalle.
3. Playwright verifica estados disponible, agotado y caducado, controles táctiles y scroll.

### Verde mínimo

1. Separar el formulario de alta y la colección en `UiCard`/`PageSection` con jerarquía clara.
2. Mostrar lotes como cards en móvil y tabla en tablet/escritorio; reutilizar `StatusBadge`.
3. Agrupar acciones destructivas y secundarias en el detalle, conservando confirmación y estados busy.
4. Mantener cantidades, unidades, fechas y movimientos visibles y reconciliables.

### Refactor y aceptación

- Baseline de listado y detalle con estados representativos en 360 y 1440 px.
- No se modifica ningún DTO, endpoint ni consulta de inventario.

## 11.7 — Catálogos y páginas secundarias

### Rojo

1. Parametrizar los tests existentes de Ingredientes, Unidades, Etiquetas y Tipos de comida para estados list, edit, error y vacío.
2. Verificar que las cards móviles y tablas de escritorio exponen las mismas acciones y nombres accesibles.
3. Cubrir NotFound, Error y reconexión con contenido español y acción clara.

### Verde mínimo

1. Migrar los cuatro catálogos usando primitives compartidos, evitando crear un componente CRUD genérico que oculte diferencias.
2. Cards compactas en móvil y tablas semánticas en tablet/escritorio; formularios apilados y acciones consistentes.
3. Aplicar tokens y componentes a NotFound, Error y reconexión.
4. Eliminar CSS anterior que ya no tenga consumidores y cualquier valor visual duplicado.

### Refactor y aceptación

- Baseline representativo de un catálogo en móvil/escritorio; los otros se protegen por bUnit y estructura compartida.
- Ninguna página queda fuera del shell o utiliza confirmaciones/feedback sin estilizar.

## 11.8 — Auditoría responsive, accesibilidad y gate

1. Recorrer todas las rutas a 360×800, 768×1024 y 1440×1000; probar zoom 200 %, texto largo, vacío, error y busy.
2. Corregir overflow, orden de foco, contraste, labels, landmarks, nombres accesibles y reduced motion antes de ajustar cualquier baseline.
3. Ejecutar la matriz visual completa con datos deterministas; revisar expected/actual/diff y aceptar únicamente cambios previstos.
4. Verificar que no hay peticiones de fuentes, iconos, CSS o scripts de runtime a dominios externos.
5. Ejecutar `scripts/quality-gate.ps1`, incluyendo Component, E2E, comparación visual, `pnpm audit --audit-level high` y la auditoría NuGet existente.
6. Actualizar README con dirección visual, breakpoints, comando de baseline y reglas para crear componentes nuevos.
7. Mover ambos documentos de fase a `completed/` solo con todas las rutas migradas y el gate completo en verde.

## Matriz mínima de aceptación

| Área | 360 px | 768 px | 1440 px | Teclado | Visual |
|---|---:|---:|---:|---:|---:|
| Shell y navegación | Sí | Sí | Sí | Sí | 3 baseline |
| Inicio/recetas | Sí | Sí | Sí | Sí | Móvil/escritorio |
| Formulario/detalle receta | Sí | Muestreo | Sí | Sí | Móvil/escritorio |
| Plan semanal | Sí | Sí | Sí | Sí | 3 baseline |
| Inventario | Sí | Muestreo | Sí | Sí | Móvil/escritorio |
| Catálogos | Sí | Muestreo | Sí | Sí | 1 catálogo representativo |

## Handoff

Una evolución posterior puede insertar estadísticas como una nueva sección de Inicio consumiendo contratos propios. No debe modificar `RecipeCard`, convertir la portada en una fuente de reglas ni introducir huecos o dependencias anticipadas durante esta fase.
