# Arquitectura para contribuidores

> **Estado:** vigente · **Decisión principal:** [ADR 003](../adr/003-clean-architecture.md)

Friggy mantiene una Clean Architecture pequeña, sin CQRS, buses, microservicios ni capas adicionales. API es el composition root y Web es un cliente HTTP separado.

## Dependencias de compilación

```text
Friggy.Domain <- Friggy.Application <- Friggy.Infrastructure <- Friggy.Api
                         ^
                         |
              Friggy.Web (solo contratos)
```

| Proyecto | Responsabilidad | Puede conocer |
|---|---|---|
| `Friggy.Domain` | Entidades, invariantes y objetos de valor | ningún proyecto |
| `Friggy.Application` | Casos de uso, DTO y puertos | Domain y abstracciones mínimas de DI para `AddApplication` |
| `Friggy.Infrastructure` | EF Core, PostgreSQL, repositorios, migraciones | Application y Domain |
| `Friggy.Api` | HTTP, OpenAPI, `ProblemDetails` | Application e Infrastructure |
| `Friggy.Web` | Blazor, estado de interfaz y clientes HTTP | Contratos inmutables de Application |

## Flujo de ejecución

Las flechas del flujo no representan referencias de proyectos:

```text
Componente Blazor
    -> cliente HttpClient tipado
    -> llamada a un endpoint de tipo minimal API
    -> uso de un servicio de la capa de application
    -> uso de un puerto de repositorio
    -> ejecución del adaptador que se conecta a PostgreSQL
    -> recupera los datos o realiza una operación
```

- API registra Application e Infrastructure.
- Un endpoint traduce HTTP y delega; no contiene reglas de negocio ni ejecuta migraciones.
- Application coordina casos de uso mediante interfaces.
- Domain conserva las reglas de negocio.

## Capacidades de negocio

El código se organiza alrededor de cuatro capacidades principales y una política transversal de mediciones:

* **Catálogos y recetas:** referencias compartidas, entidad principal receta y sus clasificaciones.
* **Planificación diaria:** planes por fecha, huecos, asignaciones, raciones, horarios, estados y plantillas reutilizables.
* **Inventario y finalización:** lotes, movimientos, comparación de necesidades y consumo transaccional.
* **Lista de la compra:** read model por intervalo que agrega comidas pendientes y descuenta inventario utilizable.

`MeasurementCalculator` y los objetos de valor de unidades proporcionan una política compartida para masa, volumen, conteo y unidades sin conversión. No constituyen una capa ni un servicio externo.

## Reglas de cambio

* Una validación de formulario mejora la respuesta inmediata, pero una invariante debe permanecer en Domain.
* Los conflictos de referencias y la coordinación entre entidades pertenecen a Application.
* Las consultas, el tracking y la concurrencia pertenecen a Infrastructure.
* La API usa DTO de Application y devuelve semántica HTTP estable mediante `ProblemDetails`.
* Los componentes no llaman directamente a repositorios ni almacenan reglas de negocio.
* Un cambio de esquema necesita migración explícita e integración con PostgreSQL real.
* La cancelación se propaga desde el ciclo de vida de Blazor hasta EF Core.

Los recorridos técnicos muestran estas reglas sobre casos completos:

* [Creación de una receta](recipe-creation-walkthrough.md)
* [Planificación diaria](daily-planning-walkthrough.md)
* [Finalización con inventario](inventory-completion-walkthrough.md)
