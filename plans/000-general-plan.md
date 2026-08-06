# Plan general del MVP de Friggy

## Objetivo

Entregar una aplicación monousuario ejecutada localmente que permita completar el recorrido:

> Crear ingredientes y catálogos, crear una receta con ingredientes y pasos, crear una semana y asignar recetas a sus comidas.

Se prioriza el alcance completo frente al límite inicial de diez días. La estimación de referencia es de 12 a 15 días laborables con TDD, Clean Architecture, frontend separado y pruebas end-to-end.

Quedan fuera del MVP la autenticación, imágenes, IA, información nutricional, frigorífico virtual, lista de la compra y despliegue público.

## Arquitectura objetivo

El backend seguirá Clean Architecture y el frontend se comunicará exclusivamente por HTTP:

```text
Friggy.Domain
    ↑
Friggy.Application
    ↑
Friggy.Infrastructure
    ↑
Friggy.Api

Friggy.Web ──HTTP──> Friggy.Api
```

| Proyecto | Responsabilidad | Dependencias permitidas |
|---|---|---|
| `Friggy.Domain` | Entidades, invariantes, excepciones y objetos de valor | Ninguna |
| `Friggy.Application` | Casos de uso, puertos de persistencia y contratos inmutables | Domain |
| `Friggy.Infrastructure` | EF Core, PostgreSQL, repositorios, migraciones y seed | Application, Domain |
| `Friggy.Api` | Minimal APIs, HTTP, OpenAPI, `ProblemDetails` y composition root | Application, Infrastructure |
| `Friggy.Web` | Blazor Web App Interactive Server y clientes HTTP tipados | Contratos de Application |

Domain no contendrá referencias a EF Core o ASP.NET Core. Application no conocerá PostgreSQL. Web no accederá a `DbContext`, repositorios o entidades persistentes.

## Modelo de datos del MVP

- `Ingredient`: identificador y nombre único.
- `UnitType`: identificador, nombre y símbolo únicos.
- `RecipeTag`: identificador y nombre único.
- `MealType`: identificador, nombre único y orden.
- `Recipe`: nombre, tiempo estimado y colecciones ordenadas.
- `RecipeIngredient`: receta, ingrediente, cantidad positiva, unidad y orden.
- `RecipeStep`: receta, descripción, tiempo opcional y posición.
- Relación muchos a muchos entre `Recipe` y `RecipeTag`.
- Relación muchos a muchos entre `Recipe` y `MealType` para recomendaciones.
- `WeeklyPlan`: nombre, lunes de inicio, descripción y final calculado a siete días.
- `MealPlanEntry`: fecha, tipo de comida y receta; combinación única por plan, fecha y tipo.

Todos los nombres se recortarán, serán obligatorios y se compararán sin distinguir mayúsculas para aplicar unicidad. Las cantidades serán decimales positivas; tiempos y posiciones no admitirán valores negativos.

## Contratos HTTP

Los grupos de rutas serán:

- `/api/ingredients`
- `/api/unit-types`
- `/api/recipe-tags`
- `/api/meal-types`
- `/api/recipes`
- `/api/weekly-plans`

Cada grupo ofrecerá listado, consulta por identificador, creación, actualización y borrado cuando corresponda. API expondrá DTO, nunca entidades de Domain. Se utilizarán `TypedResults`, grupos de rutas, `CancellationToken`, OpenAPI y `ProblemDetails` consistente para validación, no encontrado, duplicado y conflicto por referencias.

## Metodología TDD

Todo comportamiento de Domain, Application, repositorios, migraciones y Minimal APIs seguirá:

1. Elegir el criterio de aceptación más pequeño.
2. Escribir un test xUnit v3 con estructura Arrange–Act–Assert.
3. Ejecutarlo y observar un rojo debido al comportamiento ausente.
4. Implementar el mínimo código de producción.
5. Obtener verde en el test focalizado.
6. Refactorizar manteniendo el comportamiento.
7. Ejecutar la suite de capa y después las suites dependientes.
8. Incorporar cambios solamente con todo verde.

Los defectos se reproducirán primero con un test rojo. Scaffold, migraciones generadas y configuración declarativa quedan fuera del TDD estricto, pero tendrán pruebas de arquitectura, integración o smoke tests.

## Estrategia de pruebas

| Proyecto | Herramientas | Cobertura conductual |
|---|---|---|
| `Friggy.Domain.Tests` | xUnit v3 | Invariantes y comportamiento de agregados |
| `Friggy.Application.Tests` | xUnit v3 y fakes manuales | Casos de uso, errores y cancelación |
| `Friggy.IntegrationTests` | xUnit v3, `WebApplicationFactory`, Testcontainers | EF Core, migraciones, PostgreSQL y API |
| `Friggy.ComponentTests` | bUnit sobre xUnit v3 | Formularios, estados, navegación y errores Blazor |
| `Friggy.EndToEndTests` | Playwright sobre xUnit v3 | Recorrido principal en Chromium |

Las pruebas de persistencia usarán PostgreSQL real en un contenedor efímero. No se empleará EF Core InMemory como sustituto. No habrá un porcentaje de cobertura obligatorio: toda regla y comportamiento público deberá estar probado, sin tests omitidos ni dependencias de orden.

## Fases

1. [Arquitectura y banco de pruebas](phases/01-architecture-and-test-harness.md): solución, límites, PostgreSQL, automatización y runner.
2. [Catálogos](phases/02-catalogs.md): ingredientes, unidades, etiquetas y tipos de comida.
3. [Recetas](phases/03-recipes.md): agregado, ingredientes, etiquetas, pasos, API y UI.
4. [Planificación semanal](phases/04-weekly-planning.md): semanas, entradas y calendario.
5. [Integración completa](phases/05-full-integration.md): comunicación Web–API, bUnit y Playwright.
6. [Estabilización y piloto](phases/06-stabilization-and-pilot.md): calidad, regresión, documentación y sesión piloto.

## Automatización local

- `scripts/setup.ps1`: comprobar .NET 10, Docker y Compose; restaurar; iniciar PostgreSQL; aplicar migraciones; instalar Chromium de Playwright.
- `scripts/start.ps1`: iniciar PostgreSQL, API y Web, esperar health checks y mostrar las URL.
- `scripts/test.ps1`: ejecutar Domain, Application, Integration, Component y End-to-End en ese orden, deteniéndose ante el primer fallo.

Los scripts serán idempotentes, no sobrescribirán configuración local ni eliminarán volúmenes y devolverán códigos de salida accionables. La configuración se proporcionará mediante variables de entorno; no se versionarán secretos.

## Agentes y skills de managedcode/dotnet-skills

La selección sigue el enrutamiento de [`dotnet-router`](https://github.com/managedcode/dotnet-skills/blob/main/catalog/Platform/DotNet/agents/dotnet-router/AGENT.md):

| Área | Agente | Skills principales |
|---|---|---|
| Estructura y arquitectura | `dotnet-router`, `dotnet-review` | `architecture`, `project-setup`, `modern-csharp` |
| ASP.NET Core y API | `dotnet-router` | `aspnet-core`, `minimal-apis`, `microsoft-extensions` |
| Datos | `dotnet-data` | `entity-framework-core`; `optimizing-ef-core-queries` solo al estabilizar |
| Frontend | `dotnet-frontend` | `blazor`, `playwright-visual-testing` |
| Testing | `code-testing-planner`, `test-quality-auditor` | `xunit`, `run-tests`, `assertion-quality`, `test-anti-patterns` |
| Build y calidad | `dotnet-build`, `dotnet-review` | `quality-ci`, `code-review`, `coverage-analysis` |

El comando `dotnet skills recommend` se ejecutó sobre los diez proyectos generados. Detectó como recomendaciones de alta confianza `aspnet-core`, `blazor`, `entity-framework-core`, `minimal-api-file-upload` y `optimizing-ef-core-queries`; de confianza media `microsoft-extensions`, `playwright-visual-testing` y `project-setup`; y de confianza baja `dotnet` y `modern-csharp`.

Decisiones sobre el resultado automático:

- Adoptar las recomendaciones alineadas con las tecnologías actuales.
- Aplazar `minimal-api-file-upload`, porque las imágenes están fuera del MVP.
- Reservar `optimizing-ef-core-queries` para consultas medidas durante la fase 6, evitando optimización prematura.
- Añadir manualmente `architecture`, `minimal-apis`, `xunit`, `quality-ci` y `code-review`, porque responden a decisiones explícitas del proyecto aunque el escáner no las dedujera.

Se instalaron a nivel de proyecto, en `.codex/skills`, las 17 skills que gobernarán los ejemplos y la futura ejecución: `architecture`, `aspnet-core`, `assertion-quality`, `blazor`, `code-review`, `coverage-analysis`, `dotnet`, `entity-framework-core`, `microsoft-extensions`, `minimal-apis`, `modern-csharp`, `playwright-visual-testing`, `project-setup`, `quality-ci`, `run-tests`, `test-anti-patterns` y `xunit`.

También se instalaron en `.codex/agents` siete perfiles ejecutores: `dotnet-router`, `dotnet-build`, `dotnet-data`, `dotnet-frontend`, `dotnet-review`, `code-testing-planner` y `test-quality-auditor`. Codex debe reiniciarse para descubrirlos como agentes nativos en una nueva sesión; sus reglas ya se aplicaron a estas guías tras revisar el mismo catálogo antes de redactar los ejemplos.

## Definición de terminado

Una tarea está terminada cuando:

- Se observó el test rojo antes del comportamiento funcional.
- El cambio mínimo y su refactorización mantienen las suites verdes.
- Las dependencias entre proyectos respetan Clean Architecture.
- Las migraciones funcionan desde una base vacía.
- API e interfaz exponen el comportamiento acordado.
- No existen warnings de paquetes vulnerables aceptados sin una decisión documentada.
- Los scripts aplicables finalizan correctamente.
- La documentación y el plan de fase están actualizados.

## Riesgos y reglas de recorte

- El alcance funcional tiene prioridad sobre la fecha; una desviación extiende el calendario.
- No se reducirán las pruebas para recuperar tiempo.
- Las optimizaciones, pulido visual adicional y administración avanzada de catálogos se ejecutan después del recorrido principal.
- La advertencia de vulnerabilidad de paquetes observada durante el scaffold debe resolverse en la fase 1 antes de funcionalidad.

## Estado actual

La fase 1 quedó completada el 6 de agosto de 2026. Los cinco proyectos de producción y los cinco proyectos de pruebas están validados; PostgreSQL, EF Core, xUnit v3/MTP, bUnit, Testcontainers, Playwright y los scripts locales superan la puerta de calidad. La fase 2 está habilitada y su siguiente unidad ejecutable es la subfase 2.1.
