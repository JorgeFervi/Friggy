# ADR 004: TDD y estrategia de pruebas

* **Estatus:** Aceptado
* **Fecha:** 2026-08-06
* **Autor:** Jorge Fervi

## Contexto

El MVP debe construirse en un plazo corto sin sacrificar la capacidad de cambiar reglas, persistencia, API o interfaz con seguridad. La base de datos elegida es PostgreSQL y el frontend es una aplicación Blazor separada, por lo que las pruebas deben cubrir tanto los límites de cada capa como el recorrido real por HTTP.

## Decisión

Todo comportamiento funcional y toda corrección de defecto seguirá el ciclo obligatorio:

> test xUnit v3 en rojo → implementación mínima → verde → refactorización

xUnit v3 se ejecutará sobre Microsoft Testing Platform con `dotnet test`. La estrategia por nivel será:

* `Friggy.Domain.Tests`: invariantes y comportamiento de entidades y objetos de valor.
* `Friggy.Application.Tests`: casos de uso con fakes de repositorio escritos a mano, sin contenedor de dependencias.
* `Friggy.IntegrationTests`: repositorios, migraciones y Minimal APIs con `WebApplicationFactory` y PostgreSQL real mediante Testcontainers. Cada clase utiliza una base física aislada, aplica migraciones y restablece su esquema entre tests.
* `Friggy.ComponentTests`: componentes Blazor con bUnit sobre xUnit v3, un `BunitContext` por test y dobles HTTP escritos a mano.
* `Friggy.EndToEndTests`: recorrido completo con Playwright para .NET y Chromium. Cada test dispone de un `BrowserContext` aislado y conserva trace, captura, vídeo y logs cuando falla.

No se usará EF Core InMemory como sustituto de PostgreSQL. No se impondrá un porcentaje arbitrario de cobertura: toda regla y comportamiento público deberá estar probado. No se admitirán tests omitidos, tautológicos, dependientes del orden, de esperas temporales fijas o de datos mutables compartidos.

El scaffold, las migraciones generadas y la configuración declarativa quedan fuera del TDD estricto, pero estarán protegidos mediante pruebas arquitectónicas, de integración, de contrato o smoke tests. Los comandos de filtrado seguirán la sintaxis de MTP en .NET 10 (`--filter-class`, `--filter-method` y `--filter-trait`) sin el separador `--`.

## Consecuencias

* **Positivo:** Cada cambio funcional comienza con evidencia ejecutable del comportamiento esperado.
* **Positivo:** Las diferencias reales de PostgreSQL, EF Core, HTTP y navegador se detectan antes del recorrido manual.
* **Positivo:** Las suites rápidas de Domain y Application mantienen ciclos de desarrollo cortos.
* **Negativo/Riesgo:** Testcontainers, la API, Blazor y Chromium incrementan el tiempo y los requisitos de las suites externas.
* **Negativo/Riesgo:** Observar y conservar la secuencia rojo-verde exige cambios pequeños y disciplina durante todo el desarrollo.
* **Negativo/Riesgo:** Los tests E2E se reservarán para recorridos críticos; duplicar en ellos todas las combinaciones cubiertas por niveles inferiores haría la suite lenta y frágil.
