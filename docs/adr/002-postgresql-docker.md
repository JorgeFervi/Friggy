# ADR 002: Uso de PostgreSQL en Docker para la persistencia

* **Estatus:** Aceptado
* **Fecha:** 2026-08-03
* **Autor:** Jorge Fervi

## Contexto

Friggy necesita una base de datos relacional para almacenar recetas, ingredientes, etiquetas, unidades y planes semanales. Aunque el MVP se ejecutará localmente, el modelo contiene relaciones de muchos a muchos y debe poder evolucionar hacia una aplicación desplegada sin sustituir el motor de persistencia.

El entorno local también debe ser reproducible y sencillo de preparar durante el plazo de desarrollo.

## Decisión

Se utilizará **PostgreSQL** como sistema gestor de base de datos y **Entity Framework Core** como capa de acceso a datos. Durante el desarrollo, PostgreSQL se ejecutará en un contenedor administrado mediante Docker Compose y conservará los datos en un volumen persistente.

La configuración local se proporcionará mediante variables de entorno y un archivo `appsettings.Development.json` excluido del control de versiones. El repositorio incluirá un `appsettings.json` sin secretos y scripts PowerShell para preparar y arrancar el entorno.

## Consecuencias

* **Positivo:** El mismo motor podrá emplearse en desarrollo y en un futuro despliegue, reduciendo diferencias de comportamiento y migraciones posteriores.
* **Positivo:** Docker Compose hará reproducible la versión, configuración básica y comprobación de salud de PostgreSQL.
* **Positivo:** PostgreSQL y Entity Framework Core soportan las restricciones, relaciones y transacciones necesarias para el dominio.
* **Negativo/Riesgo:** Docker pasa a ser un requisito local y aumenta ligeramente el tiempo de preparación frente a una base de datos embebida.
* **Negativo/Riesgo:** Los scripts deberán tratar explícitamente contenedores no disponibles, puertos ocupados, credenciales locales y tiempos de arranque.
