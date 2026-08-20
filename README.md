# Friggy

Friggy es una aplicación web para organizar la cocina doméstica desde un único lugar: recetas, planificación semanal e inventario de alimentos. Su objetivo es facilitar la decisión de qué cocinar, aprovechar lo que ya hay disponible y reducir el desperdicio.

Actualmente se ejecuta de forma local y está orientada a un único usuario.

## ¿Qué puedes hacer con Friggy?

* Crear, editar y eliminar recetas con tiempo estimado, etiquetas, tipos de comida, cantidades, unidades y pasos ordenados.
* Registrar ingredientes, unidades, etiquetas y tipos de comida.
* Indicar qué ingredientes se utilizan en cada paso de una receta.
* Crear, editar y eliminar planes semanales, y configurar para cada día sus comidas, orden, horario, receta y número de raciones.
* Marcar una comida como omitida o completarla para registrar si estamos siguiendo el plan que nos hemos propuesto.
- Gestionar el inventario doméstico.
* Registrar alimentos por lotes con cantidad, unidad y fecha de caducidad.
* Consultar el estado y el historial de movimientos de cada lote o ingrediente, además de consumirlo, descartarlo, ajustar su cantidad real o corregir su caducidad.
* Comparar lo necesario para un plan semanal con las cantidades que tenemos en casa y detectar cantidades faltantes.

## Quick start: instalación

Los scripts de desarrollo local están verificados en Windows con PowerShell. Antes de empezar, instala y deja disponibles en `PATH`:

* Git.
* .NET SDK 10.0.302 o un parche compatible de la serie 10.0.3xx.
* Docker Desktop o Docker Engine con Docker Compose, en ejecución.
* Node.js 24 LTS.
* pnpm 11.19.0.

Clona el repositorio, entra en su directorio y ejecuta la preparación inicial:

```powershell
git clone https://github.com/JorgeFervi/Friggy.git
Set-Location Friggy
./scripts/setup.ps1
```

`setup.ps1` valida que tengas las herramientas instaladas, inicia el contenedor PostgreSQL (en caso de no existir, lo crea), aplica las migraciones en la base de datos y prepara las pruebas E2E. El script se puede volver a ejecutar sin borrar los datos locales.

## Quick start: iniciar el proyecto

Después de completar la instalación, inicia PostgreSQL, la API y la aplicación web con:

```powershell
./scripts/start.ps1
```

Cuando el script termine, abre [http://localhost:5179](http://localhost:5179) en el navegador. La API queda disponible en [http://localhost:5292](http://localhost:5292).

Para detener la aplicación y PostgreSQL:

```powershell
./scripts/stop.ps1
```

El volumen de PostgreSQL conserva los datos entre ejecuciones. En los siguientes arranques basta con ejecutar `./scripts/start.ps1`; repite `setup.ps1` cuando cambien las dependencias o las migraciones.

## Tecnologías principales

* **Backend:** ASP.NET Core con Minimal APIs sobre .NET 10.
* **Frontend:** Blazor Web App con interactividad en servidor.
* **Persistencia:** PostgreSQL ejecutado localmente mediante Docker Compose.
* **Acceso a datos:** Entity Framework Core con migraciones versionadas.
* **Pruebas:** xUnit v3 sobre Microsoft Testing Platform, bUnit (para componentes visuales Blazor), Testcontainers y Playwright para pruebas E2E.

## Arquitectura

La frontend `Friggy.Web` consume `Friggy.Api` exclusivamente por HTTP. La API aplica los casos de uso de la capa `Friggy.Application` y persiste los datos usando `Friggy.Infrastructure` en PostgreSQL. La aplicación se sostiene sobre Clean-Archiecture siguiendo la siguiente dirección de dependencias:

```text
Friggy.Web ──HTTP──> Friggy.Api ──> Application ──> Infrastructure ──> PostgreSQL/Docker
```

El recorrido completo de creación de una receta está explicado en [docs/development/recipe-creation-walkthrough.md](docs/development/recipe-creation-walkthrough.md).

## Pruebas

Para compilar una vez y ejecutar todas las suites en orden:

```powershell
./scripts/test.ps1
```

El script usa la configuración `Release` para no interferir con los procesos locales `Debug` iniciados por `start.ps1`, y termina inmediatamente si falla cualquier comando o suite. El parámetro `-SkipBuild` permite reutilizar una compilación previa desde el gate completo.

## Futuros cambios

El proyecto recibirá futuras actualizaciones incluyendo lo siguiente:
- Multiusuario con authenticación.
- Inteligencia artificial para la sugerencia de nuevas recetas, recomendaciones a la hora de preparar un plato, ingredientes alternativos, etc. 
- Información nutricional
- Imágenes
- Conversiones entre unidades
- Lista de la compra