# ADR 003: Clean Architecture con frontend separado

* **Estatus:** Aceptado
* **Fecha:** 2026-08-06
* **Autor:** Jorge Fervi

## Contexto

Friggy necesita evolucionar durante un MVP corto sin acoplar las reglas de negocio a ASP.NET Core, Entity Framework Core, PostgreSQL o Blazor. También necesita pruebas focalizadas por capa y la posibilidad de sustituir detalles externos sin reescribir el dominio.

## Decisión

El backend seguirá Clean Architecture mediante cuatro proyectos y la interfaz residirá en un quinto proyecto separado:

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

Las responsabilidades y dependencias permitidas son:

* `Friggy.Domain`: entidades, invariantes, excepciones y objetos de valor. No referencia otros proyectos ni frameworks externos.
* `Friggy.Application`: casos de uso, puertos de persistencia y contratos inmutables. Referencia Domain. Como excepción deliberada, puede usar `Microsoft.Extensions.DependencyInjection.Abstractions` únicamente para exponer su extensión de registro `AddApplication`.
* `Friggy.Infrastructure`: EF Core, PostgreSQL, repositorios y migraciones. Referencia Application y Domain.
* `Friggy.Api`: Minimal APIs, traducción HTTP, OpenAPI, `ProblemDetails` y composition root. Referencia Application e Infrastructure y es el único proyecto que registra Infrastructure.
* `Friggy.Web`: Blazor Web App. Puede compartir contratos inmutables de Application, pero accede a la funcionalidad exclusivamente por HTTP; no conoce Infrastructure, `DbContext`, repositorios ni entidades persistentes.

Las referencias se protegerán mediante pruebas arquitectónicas. No se introducirán CQRS, un bus de mensajes, microservicios ni capas adicionales mientras el dominio y la carga no aporten evidencia que los justifique.

## Consecuencias

* **Positivo:** Las reglas de negocio y los casos de uso pueden probarse sin infraestructura ni servidor web.
* **Positivo:** PostgreSQL, EF Core, ASP.NET Core y Blazor permanecen como detalles sustituibles en los límites externos.
* **Positivo:** Las dependencias prohibidas se detectan automáticamente antes de incorporar cambios.
* **Negativo/Riesgo:** La solución contiene más proyectos y mapeos que una aplicación monolítica pequeña.
* **Negativo/Riesgo:** Compartir contratos de Application con Web exige mantenerlos libres de detalles internos y de persistencia.
* **Negativo/Riesgo:** La excepción de abstracciones de inyección de dependencias en Application debe permanecer limitada al registro de servicios para no convertir el contenedor en una dependencia de los casos de uso.

## Validación del piloto

El piloto técnico local del 10 de agosto de 2026 recorrió la aplicación mediante `Friggy.Web`, verificó las llamadas HTTP a `Friggy.Api` y confirmó la persistencia PostgreSQL tras reiniciar los servicios. No se introdujeron referencias nuevas ni capas adicionales; el registro está en [plans/pilot/20260810-local-technical-pilot.md](../../plans/pilot/20260810-local-technical-pilot.md).
