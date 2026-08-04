# Fase 3 — Recetas

- **Estado:** Bloqueada por fase 2
- **Estimación:** 4 días
- **Dependencias:** [Fase 2](02-catalogs.md)
- **Guía ejecutable:** [Implementación de la fase 3](../implementation/03-recipes-implementation.md)

## Resultado esperado

Crear, consultar, editar y borrar recetas con ingredientes cuantificados, unidades, etiquetas, tipos de comida recomendados y pasos ordenados.

## Precondiciones

- Catálogos persistidos y accesibles por API.
- Política de conflictos de borrado y orden de colecciones verificada.

## Orden de ejecución

1. Escribir tests rojos del agregado `Recipe` y sus invariantes.
2. Implementar dominio mínimo para ingredientes, pasos, etiquetas y tipos de comida.
3. Escribir tests rojos de creación, consulta, actualización y borrado en Application.
4. Implementar casos de uso y mapeo a DTO inmutables.
5. Escribir tests de integración rojos para relaciones, transacción y carga del agregado.
6. Implementar mapeos EF Core, repositorio, proyecciones y migración.
7. Escribir tests HTTP rojos y exponer `/api/recipes` mediante route group.
8. Crear cliente HTTP, listado, formulario y detalle Blazor con bUnit.
9. Ejecutar el recorrido de receta desde UI hasta PostgreSQL.

## Entregables

- Agregado `Recipe` con colecciones encapsuladas.
- Casos de uso transaccionales y contratos de receta.
- Tablas de ingredientes, pasos y relaciones muchos a muchos.
- API y OpenAPI de recetas.
- UI de listado, formulario y detalle.

## Criterios de salida

- Una receta exige nombre y al menos un ingrediente y un paso para considerarse completa.
- Cantidades son positivas; posiciones son únicas y no negativas; tiempos no son negativos.
- Catálogos inexistentes producen validación y no escrituras parciales.
- La lectura devuelve ingredientes y pasos ordenados y usa consultas sin tracking.
- Un fallo en cualquier relación revierte la operación completa.
- Suites de Domain, Application, Integration y Component en verde.

## Agentes y skills

- `dotnet-data`: agregado persistente, relaciones y transacción.
- `dotnet-review`: diseño del agregado, async/cancelación y casos negativos.
- `dotnet-frontend`: formulario dinámico y estados de carga/error.
- Skills: `architecture`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`.

## Handoff

Guardar datos de ejemplo reproducibles, verificar el contrato de lectura y habilitar [Fase 4 — Planificación semanal](04-weekly-planning.md).
