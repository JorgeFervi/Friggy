# Instalación y primer arranque

> **Estado:** vigente · **Entorno verificado:** Windows con PowerShell

Esta guía prepara Friggy para uso y desarrollo local. PostgreSQL, API y Web se ejecutan en tu equipo; los datos no se envían a un servicio externo.

## Requisitos

Instala las siguientes herramientas y asegúrate de que sus comandos estén disponibles en `PATH`:

* Git.
* .NET SDK 10.0.302 o un parche compatible de la serie 10.0.3xx.
* Docker Desktop o Docker Engine con Docker Compose.
* Node.js 24 LTS.
* pnpm 11.19.0.

Puedes comprobarlas desde PowerShell:

```powershell
git --version
dotnet --version
docker version
docker compose version
node --version
pnpm --version
```

Docker debe estar iniciado y `docker info` debe terminar correctamente.

## Preparar el repositorio

```powershell
git clone https://github.com/JorgeFervi/Friggy.git
Set-Location Friggy
./scripts/setup.ps1
```

El script realiza estas operaciones:

1. Valida `dotnet`, `docker`, `node` y `pnpm`.
2. Instala las dependencias exactas de `pnpm-lock.yaml`.
3. Inicia PostgreSQL y espera a que esté saludable.
4. Restaura las herramientas y paquetes .NET.
5. Aplica las migraciones de Entity Framework Core.
6. Compila el proyecto E2E e instala Chromium para Playwright.

Puedes repetir `setup.ps1`: no elimina el volumen de PostgreSQL ni los datos existentes.

## Iniciar Friggy

```powershell
./scripts/start.ps1
```

El script inicia lo que todavía no esté disponible y muestra las direcciones finales:

| Servicio | Dirección |
|---|---|
| Aplicación web | [http://localhost:5179](http://localhost:5179) |
| API | [http://localhost:5292](http://localhost:5292) |
| Salud de la API | [http://localhost:5292/health](http://localhost:5292/health) |
| OpenAPI JSON | [http://localhost:5292/openapi/v1.json](http://localhost:5292/openapi/v1.json) |

Los procesos se ejecutan en segundo plano y escriben sus logs en `.friggy/logs`.

## Detener Friggy

```powershell
./scripts/stop.ps1
```

Se detienen Web, API y PostgreSQL, pero el volumen de base de datos se conserva. En el próximo uso solo necesitas ejecutar `./scripts/start.ps1`.

## Continuar

* Sigue [Tu primera semana con Friggy](first-steps.md) para aprender el recorrido principal.
* Consulta [Resolución de problemas](troubleshooting.md) si algún script no termina correctamente.
* Revisa [Persistencia y gestión de datos](data-management.md) antes de restablecer la base de datos.
