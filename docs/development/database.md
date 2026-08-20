# Persistencia y migraciones

> **Estado:** vigente · **Decisión principal:** [ADR 002](../adr/002-postgresql-docker.md)

PostgreSQL es el gestor de base de datos que utiliza este desarrollo y sus pruebas de integración y E2E. EF Core InMemory no sustituye ninguna prueba de persistencia.

## Entorno local

`compose.yaml` define PostgreSQL, su health check y un volumen persistente. Los valores locales son deliberadamente conocidos y no deben reutilizarse fuera del equipo de desarrollo.

La API lee `ConnectionStrings:Friggy`, que puede sobrescribirse con:

```powershell
$env:ConnectionStrings__Friggy = 'Host=localhost;Port=5432;Database=friggy;Username=friggy;Password=valor-local'
```

La factoría de EF Core lee `FRIGGY_CONNECTION_STRING` y, si falta, usa la conexión local de Docker Compose.

## Crear una migración

Un cambio de modelo comienza con pruebas de dominio o aplicación cuando modifica comportamiento. Después actualiza la configuración EF y genera una migración explícita:

```powershell
dotnet ef migrations add NombreDescriptivo --project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj --startup-project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj --context FriggyDbContext --output-dir Persistence/Migrations
```

Revisa el archivo generado y el snapshot. No edites migraciones ya aplicadas para cambiar su significado; añade una migración posterior.

## Aplicar migraciones

La preparación local las aplica mediante:

```powershell
./scripts/setup.ps1
```

El comando equivalente es:

```powershell
dotnet ef database update --project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj --startup-project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj --context FriggyDbContext
```

Las migraciones nunca se ejecutan durante una petición HTTP. API solo registra el contexto y los repositorios en el composition root.

## Pruebas obligatorias

Todo cambio de esquema debe demostrar con PostgreSQL real:

* Migración desde una base vacía.
* Compatibilidad desde el esquema anterior cuando existan datos.
* Restricciones, índices y comportamiento de borrado.
* Transacción y concurrencia cuando intervengan varias escrituras.

Los tests de integración crean bases aisladas mediante Testcontainers y aplican las migraciones versionadas.

## Datos locales

Detener contenedores conserva el volumen. La eliminación completa se documenta, con advertencia, en [Persistencia y gestión de datos](../user-guide/data-management.md).
