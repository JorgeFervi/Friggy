# Entorno local de desarrollo

> **Estado:** vigente · **Entorno verificado:** Windows con PowerShell

## Preparación

Los requisitos y la instalación inicial se describen en [Instalación y primer arranque](../user-guide/getting-started.md). Para desarrollo, ejecuta desde la raíz:

```powershell
./scripts/setup.ps1
```

El repositorio fija el SDK mediante `global.json`, los paquetes .NET mediante `Directory.Packages.props` y las dependencias Node mediante `package.json` y `pnpm-lock.yaml`. No restaures desde fuentes diferentes a `NuGet.Config`.

## Ciclo local

```powershell
./scripts/start.ps1
./scripts/test.ps1
./scripts/quality-gate.ps1
./scripts/stop.ps1
```

`start.ps1` inicia PostgreSQL y ejecuta API y Web en segundo plano. No crea un proceso duplicado si la comprobación de salud ya responde.

| Proceso | URL | Logs |
|---|---|---|
| API | `http://localhost:5292` | `.friggy/logs/api.*.log` |
| Web | `http://localhost:5179` | `.friggy/logs/web.*.log` |
| PostgreSQL | `localhost:5432` | `docker compose logs postgres` |

Web consume de forma predeterminada `http://localhost:5292`, configurado en `FriggyApi:BaseUrl`. El timeout vive en `FriggyApi:TimeoutSeconds`.

## Configuración

La API requiere `ConnectionStrings:Friggy`. En desarrollo local, `src/Friggy.Api/appsettings.Development.json` contiene valores no secretos que coinciden con `compose.yaml`. Para sobrescribirlos en el proceso actual:

```powershell
$env:ConnectionStrings__Friggy = 'Host=servidor;Port=5432;Database=friggy;Username=usuario;Password=valor-seguro'
```

Las herramientas de diseño de EF Core usan `FRIGGY_CONNECTION_STRING` y recurren a la conexión local si la variable no está definida:

```powershell
$env:FRIGGY_CONNECTION_STRING = $env:ConnectionStrings__Friggy
```

No versiones secretos, `.env`, `.friggy`, artefactos de Playwright ni `TestResults/visual`.

## Diagnóstico

Si un proceso no inicia, revisa primero `.friggy/logs`, `docker compose ps` y `docker compose logs postgres`. La [guía de resolución de problemas](../user-guide/troubleshooting.md) contiene comprobaciones para herramientas, puertos, salud, migraciones y Playwright.
