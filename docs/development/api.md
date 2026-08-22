# API HTTP

> **Estado:** vigente

`Friggy.Api` expone Minimal APIs, OpenAPI, `ProblemDetails` y una comprobación de salud. Es el composition root del backend y no contiene reglas de negocio.

## Descubrimiento

Con el entorno local iniciado:

* Base: `http://localhost:5292`
* Salud: `GET /health`
* OpenAPI: `GET /openapi/v1.json`

OpenAPI es la fuente de los cuerpos y respuestas actuales. Este documento resume las áreas y reglas de integración.

## Grupos de rutas

| Área | Prefijo | Operaciones principales |
|---|---|---|
| Ingredientes | `/api/ingredients` | CRUD |
| Unidades | `/api/unit-types` | CRUD |
| Etiquetas | `/api/recipe-tags` | CRUD |
| Tipos de comida | `/api/meal-types` | CRUD |
| Recetas | `/api/recipes` | CRUD con ingredientes, pasos y clasificación |
| Planes | `/api/daily-plans` | Consulta por intervalo y CRUD por fecha, asignaciones, huecos, orden, hora y omisión |
| Inventario | `/api/inventory-lots` | Lista, detalle, alta, consumo, descarte, ajuste y caducidad |

Las operaciones relacionadas con inventario de un plan son:

```text
GET  /api/daily-plans/{date}/inventory-requirements
POST /api/daily-plans/{date}/meal-types/{mealTypeId}/complete
```

## Configuración web

```text
FriggyApi:BaseUrl
FriggyApi:TimeoutSeconds
```

En variables de entorno se expresan como `FriggyApi__BaseUrl` y `FriggyApi__TimeoutSeconds`.

## Errores y cancelación

Las excepciones de dominio, aplicación, referencias, concurrencia y de recursos no encontrados se traducen a `ProblemDetails` con códigos estables y estados HTTP apropiados.

El `CancellationToken` de la petición se propaga a Application y EF Core. Web cancela operaciones pendientes cuando el componente deja de estar activo.

## Añadir o modificar un endpoint

1. (Opcional) Define primero la nueva entidad y sus invariantes en la capa de Domain.
2. Define el caso de uso en la capa de Application y, opcionalmente, el puerto o un método en un puerto existente.
3. (Opcional). En la capa Infrastructure, crea la configuración en caso de que se trate de una nueva entidad y el método o clases repositorio que se conecte a la base de datos.
4. Crea un nuevo endpoint que ejecute el caso de uso definido en la capa de Application.
5. Declara nombre, respuestas exitosas y `ProblemDetails` relevantes.
6. Añade integración mediante `WebApplicationFactory`.
7. Actualiza el cliente Web y sus pruebas bUnit si la interfaz consume el cambio.
8. Verifica que OpenAPI refleje el contrato final.
