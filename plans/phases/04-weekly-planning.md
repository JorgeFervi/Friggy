# Fase 4 — Planificación semanal

- **Estado:** Completada el 10 de agosto de 2026
- **Estimación:** 2 días
- **Dependencias:** [Fase 3](03-recipes.md)
- **Guía ejecutable:** [Implementación de la fase 4](../implementation/04-weekly-planning-implementation.md)

## Resultado esperado

Crear y editar planes semanales de siete días, asignando, sustituyendo o retirando una receta para cada combinación de fecha y tipo de comida.

## Precondiciones

- Recetas completas consultables por API.
- `MealType` inicializado y ordenado.

## Orden de ejecución

1. Escribir tests rojos de fecha de inicio, rango y unicidad de entradas.
2. Implementar `WeeklyPlan` y `MealPlanEntry` hasta obtener verde.
3. Escribir tests rojos de casos de uso de creación y asignación.
4. Implementar servicios, puertos y contratos de Application.
5. Escribir tests de integración rojos para restricciones y carga semanal.
6. Implementar EF Core, migración, repositorio y proyección de calendario.
7. Escribir tests HTTP rojos y exponer `/api/weekly-plans`.
8. Implementar cliente y calendario Blazor con tests bUnit.
9. Verificar el flujo semanal completo contra PostgreSQL.

## Entregables

- Agregado semanal y reglas de asignación.
- Casos de uso y DTO de calendario de siete días.
- Restricciones PostgreSQL de unicidad y rango.
- API y pantalla de planificación semanal.

## Criterios de salida

- Solo se aceptan semanas iniciadas en lunes y las entradas quedan dentro de sus siete días.
- No existen dos entradas para el mismo plan, fecha y tipo de comida.
- Asignar de nuevo sustituye la receta de forma determinista; retirar deja el hueco vacío.
- El calendario muestra siete días y tipos ordenados, incluidos estados sin receta.
- Suites de Domain, Application, Integration y Component en verde.

## Agentes y skills

- `dotnet-data`: restricciones, consultas y migración.
- `dotnet-review`: reglas temporales y conflictos.
- `dotnet-frontend`: cuadrícula calendario y eventos.
- Skills: `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`.

## Handoff

Congelar el contrato funcional del MVP y habilitar [Fase 5 — Integración completa](05-full-integration.md).
