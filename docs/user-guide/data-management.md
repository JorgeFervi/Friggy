# Persistencia y gestión de datos

> **Estado:** vigente

## Persistencia local

PostgreSQL se ejecuta mediante Docker Compose y utiliza un volumen persistente. Estas operaciones conservan los datos:

* `./scripts/stop.ps1`
* `docker compose stop postgres`
* Reiniciar Docker o el equipo.
* Ejecutar de nuevo `./scripts/setup.ps1`.

Las migraciones pendientes se aplican durante `setup.ps1`.

## Configuración local

El repositorio incluye valores exclusivamente locales que coinciden con `compose.yaml`:

* Base de datos: `friggy`.
* Usuario: `friggy`.
* Contraseña local: `friggy_local`.
* Puerto: `5432`.

No reutilices estos valores en producción. La API lee la conexión `ConnectionStrings:Friggy`; puede sobrescribirse mediante la variable de entorno `ConnectionStrings__Friggy`.

## Restablecer todos los datos

> **Advertencia:** el siguiente procedimiento elimina de forma irreversible recetas, planes, lotes y movimientos del entorno local.

Detén la aplicación, elimina el volumen y vuelve a preparar el entorno:

```powershell
./scripts/stop.ps1
docker compose down --volumes
./scripts/setup.ps1
```

Comprueba que estás dentro del repositorio Friggy y que el proyecto de Compose mostrado por `docker compose ls` es `friggy` antes de eliminar el volumen.

## Copias y restauración

Friggy no proporciona todavía una función de copia desde la interfaz. Si necesitas conservar datos antes de un restablecimiento, utiliza las herramientas estándar `pg_dump` y `psql` de PostgreSQL o copia el volumen mediante un procedimiento administrado por Docker. Verifica siempre la restauración antes de considerar válida una copia.
