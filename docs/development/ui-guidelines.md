# Interfaz, accesibilidad y regresión visual

> **Estado:** vigente

Friggy utiliza una dirección visual cálida y gastronómica, con superficies claras, tonos azules y grisáceos y un acento cobre limitado. Inter se usa para texto y controles; Fraunces, para títulos. Fuentes, licencias, iconos, CSS y JavaScript se sirven desde `Friggy.Web` sin CDN.

## Sistema visual

* Consume colores, espacios, tipografías, radios, sombras, movimiento y breakpoints desde `wwwroot/css/tokens.css`.
* Usa componentes compartidos `Friggy*` y clases CSS con prefijo `friggy-`.
* Mantén los estilos específicos junto al componente mediante CSS isolation.
* Promueve un patrón a una hoja global solo cuando varios componentes demuestren que es compartido.
* No introduzcas una biblioteca UI ni el prefijo genérico `ui-` sin una decisión explícita.

Los componentes de presentación reciben parámetros y emiten `EventCallback`; no llaman directamente a la API ni almacenan reglas de negocio.

## Responsive

| Ancho | Composición esperada |
|---|---|
| 360–767 px | Cabecera compacta, drawer y controles apilados |
| 768–1199 px | Drawer y una o dos columnas según contenido |
| Desde 1200 px | Sidebar fija de 17 rem y contenido fluido |

Diseña primero para 360 px y verifica como mínimo 360×800, 768×1024 y 1440×1000. La página no debe producir scroll horizontal, incluido reflow con zoom al 200 %.

## Accesibilidad

* Todo control necesita nombre accesible y foco visible.
* Los objetivos táctiles deben alcanzar al menos 44×44 px.
* El contraste debe cumplir WCAG 2.2 AA.
* Respeta `prefers-reduced-motion`.
* Mantén estructura semántica, etiquetas asociadas y navegación por teclado.
* Los estados loading, vacío, error, éxito y confirmación deben ser comprensibles sin depender solo del color.

## Pruebas

Una interacción o contrato de componente se cubre con bUnit. Los recorridos críticos, responsive y visuales se cubren con Playwright. No sustituyas aserciones funcionales por una captura.

Las capturas aprobadas viven en `tests/Friggy.EndToEndTests/VisualBaselines`; `TestResults/visual` contiene resultados locales y no se versiona.

## Actualizar baselines

1. Ejecuta primero la prueba afectada sin aceptar cambios.
2. Revisa las imágenes `actual`, `expected` y `diff` en todos los viewports afectados.
3. Corrige cualquier variación no explicada.
4. Solo entonces ejecuta:

```powershell
./scripts/update-visual-baselines.ps1 -Accept
```

Nunca uses la actualización de baselines únicamente para hacer verde un cambio visual sin explicación. El gate no acepta imágenes automáticamente.
