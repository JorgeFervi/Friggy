# Fase 11 — Sistema visual y experiencia responsive

- **Estado:** Completada el 18 de agosto de 2026
- **Estimación:** 12–18 días laborables
- **Dependencias:** Fases 1–10 completadas y puerta de calidad verde
- **Guía ejecutable:** [Implementación de la fase 11](../implementation/11-visual-system-and-responsive-ui-implementation.md)

## Resultado esperado

Transformar `Friggy.Web` en una aplicación cálida, gastronómica, moderna y coherente mediante componentes Razor propios. La interfaz debe funcionar desde 360 px hasta escritorio, conservar todas las rutas y comportamientos actuales y no añadir dependencias visuales de terceros.

La migración comienza por el sistema visual y el shell de navegación, continúa por Inicio, recetas, planificación e inventario y termina en catálogos y páginas secundarias. Cada incremento deja utilizables y verdes las pantallas ya migradas.

## Dirección visual aprobada

### Tipografía

- `Inter` es la tipografía de interfaz, cuerpo, formularios, tablas y controles.
- `Fraunces` en peso 600 aporta el acento gastronómico en títulos `h1` y `h2`; `Inter` se conserva en subtítulos funcionales y texto denso.
- Se versionan únicamente archivos `woff2` necesarios: Inter 400, 500, 600 y 700; Fraunces 600.
- Las fuentes y sus licencias SIL Open Font License se sirven desde `wwwroot/fonts`; no se realizan peticiones a Google Fonts ni a CDN.
- La carga usa `font-display: swap` y fallbacks del sistema para no bloquear contenido.

### Color y superficies

La paleta se centraliza en tokens CSS y se ajusta durante implementación solo si una combinación no alcanza WCAG 2.2 AA:

| Token | Valor inicial | Uso |
|---|---|---|
| `--color-primary-700` | `#284E63` | Acciones primarias activas y navegación |
| `--color-primary-600` | `#356B85` | Botones, enlaces y foco |
| `--color-primary-100` | `#DCEAF0` | Selecciones y fondos destacados |
| `--color-canvas` | `#F3F6F7` | Fondo general gris azulado |
| `--color-surface` | `#FFFFFF` | Tarjetas, formularios y paneles |
| `--color-surface-muted` | `#E8EEF1` | Secciones secundarias |
| `--color-text` | `#1E2C33` | Texto principal |
| `--color-text-muted` | `#5C6B73` | Metadatos y ayudas |
| `--color-border` | `#CBD7DC` | Divisores y campos |
| `--color-accent` | `#9F633F` | Acento cobre gastronómico de uso limitado |
| `--color-danger` | `#A63F46` | Errores y acciones destructivas |
| `--color-success` | `#3E745F` | Confirmaciones y estados correctos |

- El modo de color es únicamente claro.
- Sombras suaves, bordes finos, radios de 12–18 px y espacios amplios evocan una cocina limpia sin perder densidad funcional.
- Los iconos son SVG locales integrados mediante un componente propio; no se usa una fuente de iconos.
- Las tarjetas de receta no simulan fotografías ausentes: muestran nombre, tiempo y acciones sobre una superficie editorial con un recurso gráfico abstracto discreto.

## Arquitectura de interfaz

### Fundamentos

- Separar estilos globales en fuentes, tokens, reset/base y utilidades mínimas; mantener los estilos específicos en CSS isolation junto al componente.
- Definir escalas compartidas de espacio, tipografía, radios, sombras, ancho de contenido, capas y duración de movimiento.
- Usar unidades relativas, `clamp()` para títulos y espacios y contenedores fluidos; no fijar anchuras de página en píxeles.
- Mantener `InteractiveServer`; la fase no cambia render mode, estado compartido ni acceso HTTP.

### Componentes reutilizables iniciales

- `FriggyIcon` y `FriggyIconButton`: iconos locales, nombre accesible obligatorio cuando no hay texto visible.
- `FriggyButton`: variantes primary, secondary, ghost y danger; tamaños regular y compact; estados disabled y busy.
- `FriggyCard` y `PageSection`: superficies y composición sin acoplamiento a datos de dominio.
- `PageHeader`: título, descripción opcional, breadcrumb y zona de acciones.
- `StatusBadge`: tonos neutral, info, success, warning y danger.
- `FeedbackPanel`: loading, empty y error con título, texto y acción opcional.
- `FormField` y `FormActions`: etiqueta, ayuda, error, distribución y acciones consistentes sin ocultar los componentes `Input*` de Blazor.
- `ConfirmDialog`: sustitución accesible y estilizable de las confirmaciones destructivas del navegador.
- `RecipeCard` y `RecipeGrid`: componentes de dominio para Inicio y `/recipes`, basados en el contrato existente `RecipeListItemResponse`.

No se crea una abstracción genérica de tabla o formulario hasta que dos páginas migradas demuestren la misma estructura. Los componentes exponen parámetros y `EventCallback`; no llaman directamente a API ni almacenan estado de negocio.

### Shell responsive

- Desde 1200 px: barra lateral fija de 17 rem, contenido fluido y navegación agrupada en principal y catálogos.
- Entre 768 y 1199 px: cabecera compacta y navegación en drawer; el contenido usa una o dos columnas según espacio.
- Entre 360 y 767 px: cabecera móvil, drawer de ancho limitado, controles apilados y acciones principales de ancho completo cuando mejore la ergonomía.
- El drawer propio conserva foco, cierra con `Escape`, overlay o navegación, bloquea el scroll de fondo y anuncia estado mediante `aria-expanded` y `aria-controls`.
- Incluir enlace de salto a contenido, `NavLink` con estado activo y landmarks `header`, `nav`, `main` y `aside` correctos.

## Orden de migración

1. Fundamentos, componentes primitivos y banco de regresión visual.
2. Layout, barra lateral y drawer responsive.
3. Inicio y recorrido completo de recetas.
4. Listado, detalle y calendario de planes semanales.
5. Listado y detalle de inventario.
6. Ingredientes, unidades, etiquetas y tipos de comida.
7. Estados Error, NotFound y reconexión, revisión transversal y gate.

La portada `/` carga las recetas mediante el cliente HTTP ya existente y las presenta con `RecipeGrid`. Su composición usa secciones independientes para poder insertar más adelante estadísticas antes o junto a las recetas sin modificar `RecipeCard`, sin mostrar un hueco vacío y sin introducir ahora DTO o consultas nuevas. `/recipes` continúa existiendo y reutiliza la misma galería con sus acciones de gestión.

## Responsive y accesibilidad

- Viewports mínimos de aceptación: 360×800, 768×1024 y 1440×1000; comprobar además zoom al 200 %.
- No debe existir scroll horizontal de página; solo se admite desplazamiento local en contenido intrínsecamente tabular cuando no haya una presentación móvil equivalente.
- Recetas usan tarjetas en todos los tamaños. Planes, inventario y catálogos usan tarjetas en móvil y presentación tabular o grid en tablet/escritorio.
- Objetivo WCAG 2.2 AA: contraste, foco visible, orden de tabulación, labels, mensajes de error, objetivos táctiles mínimos de 44×44 px y semántica de tablas/listas.
- Respetar `prefers-reduced-motion`; ninguna transición es imprescindible para comprender estado o completar una acción.
- `App.razor` declara idioma español y las pantallas de error/reconexión se traducen y adaptan al sistema visual.

## Regresión visual

- Conservar los recorridos Playwright .NET y xUnit v3 como autoridad funcional.
- Añadir capturas deterministas en Chromium con locale `es-ES`, zona `Europe/Madrid`, color scheme light, device scale factor 1, animaciones deshabilitadas y datos estables.
- Usar un comparador Pixelmatch de test con `threshold` 0,15 y ratio máximo de diferencia 0,2 %. Los cambios de dimensiones siempre fallan.
- Versionar los baseline aprobados; `actual` y `diff` se guardan bajo `TestResults` y no se versionan.
- Actualizar baseline solo mediante un comando explícito y después de revisar expected, actual y diff. El gate nunca acepta capturas automáticamente.
- Cubrir como mínimo shell, Inicio/recetas, formulario y detalle de receta, calendario semanal, inventario y un catálogo en móvil y escritorio; usar tablet para validar el cambio de composición y drawer.

El comparador Node/Pixelmatch es exclusivamente tooling de pruebas, queda fijado por lockfile y no se carga en la aplicación. No se introduce Playwright Test paralelo ni se duplican recorridos TypeScript.

## Criterios de salida

- Todas las páginas usan el shell, tokens y componentes aprobados; no quedan estilos provisionales o controles sin foco visible.
- Inicio muestra recetas en cards y mantiene estados loading, error y vacío comprensibles.
- Navegación, formularios, colecciones, diálogos y acciones funcionan con teclado, ratón y tacto en los tres rangos.
- No cambian rutas, contratos HTTP, reglas de Application, persistencia ni migraciones.
- No hay fuentes, iconos, estilos o scripts de runtime servidos desde terceros.
- bUnit cubre contratos y estados de componentes; Playwright cubre navegación responsive y regresión visual.
- Build Release, formato, cinco suites, comparación visual y auditorías NuGet/npm finalizan en verde.

## Fuera de alcance

- Modo oscuro, selector de temas o personalización por usuario.
- Biblioteca UI, framework CSS, JavaScript frontend o cambio de render mode.
- Fotografías de recetas, subida de archivos, nuevas métricas o cambios de backend.
- Rediseñar reglas funcionales, textos de negocio o contratos de API salvo ajustes puramente accesibles.
