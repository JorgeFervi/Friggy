# Friggy

Friggy es una aplicación web para organizar la cocina doméstica desde un único lugar: recetas, planificación diaria e inventario de alimentos. Su objetivo es facilitar la decisión de qué cocinar, aprovechar lo que ya hay disponible y reducir el desperdicio.

Actualmente se ejecuta de forma local y está orientada a un único usuario. Incluye inventario, lista de la compra y conversiones controladas de masa, volumen y conteo.

## ¿Qué puedes hacer con Friggy?

* Administrar ingredientes, unidades, etiquetas y tipos de comida.
* Crear, editar y eliminar recetas con cantidades, unidades, etiquetas, tipos de comida y pasos ordenados.
* Asociar las líneas de ingredientes de una receta con los pasos en los que se utilizan.
* Crear un plan independiente por fecha y configurar sus comidas, orden, horario, receta y comensales.
* Guardar plantillas de planes diarios y aplicarlas de forma atómica a una o varias fechas libres.
* Omitir una comida con un motivo o completarla seleccionando los lotes que se consumen del inventario.
* Registrar existencias por lotes con cantidad, unidad y fecha de caducidad.
* Consumir, descartar o ajustar un lote, corregir su caducidad y consultar su historial de movimientos.
* Comparar necesidades e inventario mediante conversiones compatibles de masa, volumen y conteo.
* Calcular la lista de la compra para un intervalo a partir de las comidas pendientes y el inventario utilizable.

Consulta [Funcionalidades actuales](docs/product/current-capabilities.md) para conocer el alcance y las limitaciones con más detalle.

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

`setup.ps1` valida las herramientas, instala las dependencias bloqueadas de .NET y pnpm, inicia PostgreSQL, aplica las migraciones y prepara Chromium para las pruebas E2E. Es idempotente y puede ejecutarse de nuevo sin borrar los datos locales.

Consulta [Instalación y primer arranque](docs/user-guide/getting-started.md) si necesitas una explicación detallada o ayuda con el entorno.

## Quick start: iniciar el proyecto

```powershell
./scripts/start.ps1
```

Cuando el script termine:

* Abre la aplicación en [http://localhost:5179](http://localhost:5179).
* La API estará disponible en [http://localhost:5292](http://localhost:5292).
* La salud de la API podrá comprobarse en [http://localhost:5292/health](http://localhost:5292/health).
* Los logs se guardarán en `.friggy/logs`.

Para detener Web, API y PostgreSQL:

```powershell
./scripts/stop.ps1
```

El volumen de PostgreSQL conserva los datos entre ejecuciones. En los siguientes arranques basta con ejecutar `./scripts/start.ps1`; repite `setup.ps1` cuando cambien dependencias o migraciones.

## Primeros pasos

El recorrido recomendado es:

1. Revisar las diez recetas de bienvenida y sus ingredientes, unidades, etiquetas y tipos de comida.
2. Crear o adaptar una receta con sus ingredientes y pasos.
3. Registrar las existencias disponibles en el inventario.
4. Crear planes para las fechas necesarias y asignar recetas y comensales.
5. Revisar las cantidades faltantes y completar una comida seleccionando los lotes consumidos.
6. Guardar una configuración frecuente como plantilla y aplicarla a otras fechas libres.
7. Calcular la lista de la compra para los planes que continúan pendientes.

Sigue el tutorial [Tu primera semana con Friggy](docs/user-guide/first-steps.md) para completar el recorrido desde la interfaz.

## Tecnologías principales

* **Backend:** ASP.NET Core con Minimal APIs sobre .NET 10.
* **Frontend:** Blazor Web App con interactividad en servidor.
* **Persistencia:** PostgreSQL mediante Docker Compose.
* **Acceso a datos:** Entity Framework Core con migraciones versionadas.
* **Pruebas:** xUnit v3 sobre Microsoft Testing Platform, bUnit, PostgreSQL/Testcontainers y Playwright.

## Arquitectura

Las dependencias de compilación del backend apuntan hacia el dominio:

```text
Friggy.Domain <- Friggy.Application <- Friggy.Infrastructure <- Friggy.Api
                         ^
                         |
           Friggy.Web comparte contratos inmutables
```

`Friggy.Web` no accede a Infrastructure, `DbContext` ni repositorios. Durante la ejecución, toda funcionalidad atraviesa el límite HTTP:

```text
Friggy.Web -> HTTP -> Friggy.Api -> Application -> puertos -> Infrastructure -> PostgreSQL
```

La explicación completa está en [Arquitectura para contribuidores](docs/development/architecture.md) y las decisiones se conservan en [docs/adr](docs/adr).

## Pruebas y calidad

Para compilar y ejecutar las cinco suites:

```powershell
./scripts/test.ps1
```

Antes de incorporar cambios, ejecuta el gate completo:

```powershell
./scripts/quality-gate.ps1
```

El gate restaura dependencias, compila Release, verifica formato, ejecuta Domain, Application, Integration, Component y E2E, y audita vulnerabilidades de NuGet y pnpm. Consulta [Estrategia y ejecución de pruebas](docs/development/testing.md) para filtros MTP y regresión visual.

## Documentación y contribución

* [Índice de documentación](docs/README.md)
* [Guías para usuarios](docs/user-guide/getting-started.md)
* [Plantillas de planes diarios](docs/user-guide/daily-plan-templates.md)
* [Guía de contribución](CONTRIBUTING.md)
* [Estado y hoja de ruta](docs/product/roadmap.md)
* [Decisiones arquitectónicas](docs/adr)
* [Planes ejecutados](plans/README.md)

Friggy se distribuye bajo los términos de [LICENSE](LICENSE).
