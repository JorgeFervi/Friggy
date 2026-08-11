# Fase 2 — Catálogos

- **Estado:** Completada — 6 de agosto de 2026
- **Estimación:** 2 días
- **Dependencias:** [Fase 1](01-architecture-and-test-harness.md)
- **Guía ejecutable:** [Implementación de la fase 2](../implementation/02-catalogs-implementation.md)

## Resultado esperado

CRUD completo y probado para `Ingredient`, `UnitType`, `RecipeTag` y `MealType`, accesible desde PostgreSQL, Minimal APIs y Blazor.

## Precondiciones

- Fase 1 en verde y scripts operativos.
- Convenciones de identificador, normalización de nombres, errores HTTP y borrado acordadas.

## Orden de ejecución

1. Definir tests rojos de invariantes compartidas y específicas de cada catálogo.
2. Implementar entidades de dominio hasta obtener verde.
3. Definir tests rojos de casos de uso y fakes de repositorio.
4. Implementar servicios de aplicación y contratos.
5. Definir tests de integración rojos para mapeos, unicidad, migración y repositorios.
6. Implementar configuraciones EF Core, repositorios y migración.
7. Definir tests HTTP rojos y exponer route groups con `TypedResults`.
8. Añadir clientes HTTP y pantallas Blazor con tests bUnit focalizados.
9. Ejecutar suites de Domain, Application, Integration y Component.

## Entregables

- Entidades y reglas de los cuatro catálogos.
- Contratos y casos de uso CRUD cancelables.
- Esquema PostgreSQL con índices únicos normalizados.
- Endpoints `/api/ingredients`, `/api/unit-types`, `/api/recipe-tags` y `/api/meal-types`.
- Pantallas de listado, creación, edición y borrado.

## Criterios de salida

- No se admiten nombres vacíos, duplicados o fuera de longitud.
- `UnitType` valida además nombre y símbolo; `MealType` valida el orden.
- Actualización y borrado devuelven resultados tipados y conflictos coherentes.
- Las listas se devuelven ordenadas de forma determinista.
- Todos los comportamientos se desarrollaron con evidencia rojo-verde y las cuatro suites están verdes.

## Agentes y skills

- `dotnet-data`: modelado, migración, índices y consultas.
- `dotnet-review`: invariantes, contratos y cobertura negativa.
- `dotnet-frontend`: componentes Blazor y estados de UI.
- Skills: `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`, `assertion-quality`.

## Handoff

Sembrar unidades y tipos de comida iniciales, documentar sus identificadores estables y habilitar [Fase 3 — Recetas](03-recipes.md).

## Resultado del cierre

- Las cuatro verticales (`Ingredient`, `UnitType`, `RecipeTag` y `MealType`) están disponibles en Domain, Application, PostgreSQL, Minimal APIs y Blazor.
- La migración `AddCatalogs` crea índices normalizados, restricciones y los datos iniciales de unidades y tipos de comida.
- Las suites de Domain, Application, Integration y Component cubren invariantes, casos de uso, persistencia real, contratos HTTP y estados de interfaz.
- La fase 3 queda habilitada como siguiente unidad ejecutable.
