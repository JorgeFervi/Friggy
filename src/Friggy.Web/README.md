# Friggy.Web

> **Estado:** vigente · Consulta también [Arquitectura para contribuidores](../../docs/development/architecture.md) e [Interfaz, accesibilidad y regresión visual](../../docs/development/ui-guidelines.md).

`Friggy.Web` es la aplicación Blazor Web App interactiva en servidor. Presenta los recorridos de Friggy y consume `Friggy.Api` exclusivamente mediante clientes HTTP tipados. Puede compartir DTOs inmutables de Application, pero no referencia Infrastructure, `FriggyDbContext`, repositorios ni entidades persistentes.

## Organización

| Carpeta o archivo | Contenido |
|---|---|
| [Api](Api) | Interfaces, clientes HTTP tipados, opciones y traducción de `ProblemDetails`. |
| [Components/Pages](Components/Pages) | Páginas de Inicio, catálogos, recetas, planes, plantillas, inventario y lista de la compra. |
| [Components/DailyPlans](Components/DailyPlans) | Editor presentacional del plan y diálogo de finalización. |
| [Components/Recipes](Components/Recipes) | Formulario, filas, tarjetas y cuadrícula de recetas. |
| [Components/Layout](Components/Layout) | Shell, navegación responsive y reconexión de Blazor. |
| [Components/Shared](Components/Shared) | Primitivas visuales, formularios, feedback, selección y comparación de cantidades. |
| [wwwroot](wwwroot) | Tokens, estilos, fuentes e interoperabilidad JavaScript servidos localmente. |
| [Program.cs](Program.cs) | Composition root exclusivo de la Web y registro de clientes HTTP. |

## Límite HTTP

`FriggyApi:BaseUrl` determina la dirección de la API y `FriggyApi:TimeoutSeconds` el timeout de los clientes. Cada página coordina carga, guardado, cancelación y errores; los componentes de presentación reciben parámetros y emiten callbacks.

```text
Página o componente -> cliente HTTP tipado -> Friggy.Api -> Application
```

Los errores HTTP conocidos se convierten en `ApiProblemException` y después en mensajes mediante `UserFacingError`. Los componentes cancelan operaciones pendientes al finalizar su ciclo de vida.

## Interfaz y pruebas

La interfaz utiliza componentes Razor propios, CSS isolation y tokens compartidos. bUnit cubre contratos e interacciones; Playwright cubre recorridos críticos, responsive, accesibilidad y comparación visual. Los componentes no deben contener invariantes de negocio ni acceder directamente a persistencia.
