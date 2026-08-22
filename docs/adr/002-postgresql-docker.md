# ADR 002: Uso de PostgreSQL en Docker para la persistencia

* **Estatus:** Aceptado, enmendado
* **Fecha:** 2026-08-03
* **Enmienda:** 2026-08-20
* **Autor:** Jorge Fervi

## Contexto

Friggy necesita una base de datos relacional para almacenar recetas, ingredientes, etiquetas, unidades y planes diarios. Aunque el MVP se ejecutará localmente, el modelo contiene relaciones de muchos a muchos y debe poder evolucionar hacia una aplicación desplegada sin sustituir el motor de persistencia.

El entorno local también debe ser reproducible y sencillo de preparar durante el plazo de desarrollo.

## Decisión

Se utilizará **PostgreSQL** como sistema gestor de base de datos y **Entity Framework Core** como capa de acceso a datos. Durante el desarrollo, PostgreSQL se ejecutará en un contenedor administrado mediante Docker Compose y conservará los datos en un volumen persistente.

La configuración local utiliza valores conocidos y exclusivos de desarrollo, versionados en `compose.yaml` y `src/Friggy.Api/appsettings.Development.json` para que el entorno sea reproducible. No se consideran credenciales válidas fuera del equipo local y no deben reutilizarse en producción.

En tiempo de ejecución, la API lee `ConnectionStrings:Friggy`; una conexión externa o sensible se proporciona mediante `ConnectionStrings__Friggy`. Las herramientas de diseño de EF Core aceptan `FRIGGY_CONNECTION_STRING` y recurren al valor local cuando no está definido. El repositorio no incluirá secretos reales ni archivos `.env`.

## Consecuencias

* **Positivo:** El mismo motor podrá emplearse en desarrollo y en un futuro despliegue, reduciendo diferencias de comportamiento y migraciones posteriores.
* **Positivo:** Docker Compose hará reproducible la versión, configuración básica y comprobación de salud de PostgreSQL.
* **Positivo:** PostgreSQL y Entity Framework Core soportan las restricciones, relaciones y transacciones necesarias para el dominio.
* **Negativo/Riesgo:** Docker pasa a ser un requisito local y aumenta ligeramente el tiempo de preparación frente a una base de datos embebida.
* **Negativo/Riesgo:** Los scripts deberán tratar explícitamente contenedores no disponibles, puertos ocupados, credenciales locales y tiempos de arranque.
* **Negativo/Riesgo:** Versionar valores locales exige dejar claro que son únicamente datos de desarrollo y que toda conexión no local debe sobrescribirse de forma segura.

## Motivo de la enmienda

La decisión inicial preveía un `appsettings.Development.json` excluido y la generación de un `.env`. La automatización final no utiliza ese flujo: conserva una configuración local no secreta junto a Docker Compose y permite sobrescritura por entorno. La enmienda documenta la implementación real sin modificar la elección de PostgreSQL, EF Core o Docker.

## Validación del piloto

El piloto técnico local del 10 de agosto de 2026 verificó dos ejecuciones idempotentes de `scripts/setup.ps1`, la disponibilidad de PostgreSQL mediante health check y la persistencia del recorrido Web → HTTP → API tras reiniciar servicios. El detalle queda en el [cierre ejecutable de la Fase 6](../../plans/completed/implementation/06-stabilization-and-pilot-implementation.md#67--piloto-y-cierre--completada).

La guía vigente de configuración y migraciones se mantiene en [Persistencia y migraciones](../development/database.md).
