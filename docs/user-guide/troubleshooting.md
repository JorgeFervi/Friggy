# Resolución de problemas

> **Estado:** vigente · **Entorno:** Windows con PowerShell

## PowerShell bloquea los scripts

Si la política de ejecución impide iniciar un script, habilítala únicamente para la sesión actual:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

Cierra la terminal para descartar el cambio.

## Falta una herramienta

`setup.ps1` indica el comando ausente. Comprueba la instalación y `PATH`:

```powershell
Get-Command dotnet,docker,node,pnpm
dotnet --version
docker compose version
```

La versión del SDK debe ser compatible con `global.json` y pnpm con el campo `packageManager` de `package.json`.

## Docker no responde

Inicia Docker Desktop o el servicio de Docker y ejecuta:

```powershell
docker info
docker compose up --detach --wait postgres
```

Si PostgreSQL no alcanza un estado saludable, revisa:

```powershell
docker compose ps
docker compose logs postgres
```

## Un puerto está ocupado

Friggy utiliza 5179 para Web, 5292 para API y 5432 para PostgreSQL:

```powershell
Get-NetTCPConnection -LocalPort 5179,5292,5432 -State Listen
```

Detén el proceso que ya utiliza el puerto solo después de identificarlo. `start.ps1` reutiliza Web o API si sus comprobaciones HTTP ya responden.

## Web o API no inicia

Revisa los archivos:

```text
.friggy/logs/api.out.log
.friggy/logs/api.err.log
.friggy/logs/web.out.log
.friggy/logs/web.err.log
```

Comprueba también [http://localhost:5292/health](http://localhost:5292/health). Si faltan paquetes o migraciones, vuelve a ejecutar `./scripts/setup.ps1`.

## La Web no puede llamar a la API

La configuración predeterminada espera la API en `http://localhost:5292`. Confirma que esa dirección responde antes de abrir Web. Si cambias la URL, actualiza la sección `FriggyApi` de configuración o las variables equivalentes.

## No aparecen lotes disponibles

Activa **Mostrar agotados y caducados**. Si el lote aparece como caducado, revisa y corrige su fecha. Si la cantidad es cero, registra un nuevo lote o ajusta el existente a la cantidad real.

## Una necesidad no usa mi lote

El ingrediente debe coincidir y las unidades deben pertenecer a la misma dimensión. Friggy convierte, por ejemplo, kilogramos con gramos y litros con mililitros, pero nunca masa con volumen. Las unidades personalizadas sin conversión solo coinciden consigo mismas. También se excluyen lotes agotados o cuya caducidad ya pasó.

## Las pruebas E2E no encuentran Chromium

Repite `./scripts/setup.ps1`. El script compila el proyecto E2E y ejecuta el instalador de Playwright para Chromium.

## Necesito empezar con una base vacía

Consulta primero [Persistencia y gestión de datos](data-management.md). El restablecimiento del volumen elimina todos los datos locales y no se puede deshacer sin una copia válida.
