# Recorrido técnico — Creación de una receta

## Objetivo

Este recorrido permite localizar una regla o un fallo sin romper la dirección `Domain <- Application <- Infrastructure <- Api`. `Friggy.Web` permanece separado y consume la API exclusivamente por HTTP.

```text
Blazor -> cliente HTTP -> Minimal API -> Application -> Domain -> repositorio EF Core -> PostgreSQL
```

## Recorrido

1. `RecipeEdit.razor` carga los catálogos necesarios y muestra `RecipeForm`. Al recibir un envío válido, `SaveAsync` transforma `RecipeFormModel` en `CreateRecipeRequest`.
2. `RecipeApiClient.CreateAsync` serializa el contrato y envía `POST /api/recipes`. La Web no conoce `DbContext`, repositorios ni tipos de Infrastructure.
3. `RecipeEndpoints.CreateAsync` recibe el DTO, delega en `RecipeService` y devuelve `201 Created`. El endpoint no contiene reglas de negocio ni aplica migraciones.
4. `RecipeService.CreateAsync` construye el agregado, comprueba que las referencias de catálogo existan y que el nombre sea único, y coordina la persistencia.
5. `Recipe.Create`, `AddIngredient`, `AddStep` y `EnsureComplete` protegen las invariantes que deben cumplirse con independencia del cliente utilizado.
6. `RecipeRepository.AddAsync` registra el agregado en `FriggyDbContext`; `SaveChangesAsync` confirma la unidad de trabajo en PostgreSQL.
7. La respuesta recorre el camino inverso. La página navega al detalle de la receta creada y cualquier error de aplicación se convierte en un problema HTTP antes de mostrarse en la Web.

El mismo `CancellationToken` nace en el ciclo de vida del componente y se propaga por el cliente HTTP, el endpoint, el servicio y EF Core. Al abandonar la página, el componente cancela las operaciones pendientes.

## Dónde realizar cada cambio

| Necesidad | Ubicación principal | Verificación mínima |
|---|---|---|
| Texto, disposición o validación de interacción | `Friggy.Web` | bUnit |
| Invariante válida para cualquier cliente | Domain | xUnit de dominio |
| Orquestación, referencias o conflictos | Application | fake manual |
| Contrato o semántica HTTP | API y DTO compartido | integración con PostgreSQL |
| Consulta, mapeo o restricción persistida | Infrastructure | integración con PostgreSQL real |
| Recorrido visible completo | Web a PostgreSQL | Playwright |

Una validación importante no debe existir únicamente en `RecipeFormModel`: la Web ofrece respuesta inmediata, pero Domain conserva la autoridad sobre las invariantes. Un cambio de esquema requiere configuración EF Core, migración explícita y prueba de integración real.

## Comprobación de comprensión

Antes de modificar este flujo, quien lo revise debería poder responder:

- ¿Qué archivo cambiaría si el mensaje visible es confuso pero la regla es correcta?
- ¿Dónde debe vivir una regla que también afecta a clientes distintos de Blazor?
- ¿Qué capa conoce PostgreSQL y cuál conoce HTTP?
- ¿Cómo llega una cancelación desde la página hasta EF Core?
- ¿Qué pruebas deben fallar antes de corregir un defecto en cada capa?
