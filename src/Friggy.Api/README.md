# Friggy.Api

> **Estado:** vigente · Consulta también [API HTTP](../../docs/development/api.md).

La capa Friggy.Api expone los casos de uso de Friggy mediante una API HTTP
con ASP.NET Core y Minimal APIs. Actúa como composition root de la aplicación:
registra las capas de aplicación e infraestructura, configura el pipeline HTTP
y publica los grupos de endpoints.

Esta capa depende de Friggy.Application y Friggy.Infrastructure. No contiene
las reglas de negocio ni implementa directamente la persistencia; coordina la
entrada HTTP, la traducción de errores y la exposición de las respuestas.

## Organización

| Carpeta o archivo | Contenido |
|---|---|
| [Endpoints](Endpoints) | Grupos de rutas HTTP organizados por funcionalidad. |
| [Errors](Errors) | Traducción de excepciones conocidas a respuestas Problem Details. |
| [Program.cs](Program.cs) | Punto de entrada, composición de servicios y configuración del pipeline HTTP. |
| [AssemblyMarker.cs](AssemblyMarker.cs) | Tipo marcador del ensamblado de la API. |

## Composición de la aplicación

| Tipo | Representación |
|---|---|
| [Program](Program.cs) | Punto de entrada de ASP.NET Core. Construye el host, registra aplicación e infraestructura, configura el manejador de excepciones, OpenAPI y health checks, y mapea los endpoints. |
| [AssemblyMarker](AssemblyMarker.cs) | Clase marcador que permite identificar el ensamblado de la API. |

## Endpoints

| Tipo | Representación |
|---|---|
| [IngredientEndpoints](Endpoints/IngredientEndpoints.cs) | Grupo de endpoints para listar, consultar, crear, actualizar y eliminar ingredientes en /api/ingredients. |
| [MealTypeEndpoints](Endpoints/MealTypeEndpoints.cs) | Grupo de endpoints para gestionar tipos de comida en /api/meal-types. |
| [RecipeTagEndpoints](Endpoints/RecipeTagEndpoints.cs) | Grupo de endpoints para gestionar etiquetas de recetas en /api/recipe-tags. |
| [UnitTypeEndpoints](Endpoints/UnitTypeEndpoints.cs) | Grupo de endpoints para gestionar unidades de medida en /api/unit-types. |
| [RecipeEndpoints](Endpoints/RecipeEndpoints.cs) | Grupo de endpoints para listar, consultar, crear, actualizar y eliminar recetas en /api/recipes, incluidas sus asociaciones. |
| [DailyPlanEndpoints](Endpoints/DailyPlanEndpoints.cs) | Endpoints por fecha para gestionar planes diarios, asignaciones, huecos, necesidades y finalización en /api/daily-plans. |
| [DailyPlanTemplateEndpoints](Endpoints/DailyPlanTemplateEndpoints.cs) | CRUD de plantillas y aplicación atómica a fechas libres en /api/daily-plan-templates. |
| [InventoryEndpoints](Endpoints/InventoryEndpoints.cs) | Grupo de endpoints para consultar y operar sobre lotes de inventario en /api/inventory-lots. |
| [ShoppingListEndpoints](Endpoints/ShoppingListEndpoints.cs) | Consulta calculada y de solo lectura por intervalo en /api/shopping-list. |

## Manejo de errores

| Tipo | Representación |
|---|---|
| [ApiExceptionHandler](Errors/ApiExceptionHandler.cs) | Manejador global que clasifica excepciones de aplicación, dominio y persistencia, y las transforma en respuestas HTTP Problem Details con su estado y código de error. |
| [ApiFailure](Errors/ApiExceptionHandler.cs) | Registro interno que contiene el estado HTTP, título, código y detalle de un error clasificado por la API. |

## Flujo de una petición

1. Program construye la aplicación y registra las dependencias de las capas
   de aplicación e infraestructura.
2. Los grupos de Endpoints reciben la petición HTTP y delegan el caso de uso
   al servicio correspondiente de Friggy.Application.
3. El servicio aplica las reglas del dominio y utiliza los contratos de
   persistencia sin conocer los detalles de infraestructura.
4. El resultado del servicio se convierte en una respuesta HTTP tipada.
5. ApiExceptionHandler traduce las excepciones conocidas a respuestas
   Problem Details cuando se produce un error.

La API es el composition root del sistema: conecta las capas, pero mantiene las
reglas de negocio en Friggy.Domain y los detalles técnicos de persistencia en
Friggy.Infrastructure.
