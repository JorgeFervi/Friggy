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
* **Pruebas:** xUnit v3 sobre Microsoft Testing Platform, bUnit (para componentes visuales Blazor), Testcontainers y Playwright.

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

## Puerta de calidad

Antes de incorporar cambios, ejecutar:

```powershell
./scripts/quality-gate.ps1
```

El gate restaura con `NuGet.Config` y `pnpm-lock.yaml`, compila Release, verifica formato, ejecuta las cinco suites mediante Microsoft Testing Platform y falla si las auditorías de NuGet o pnpm encuentran vulnerabilidades relevantes.

## Fundamentos visuales y regresión

La interfaz sigue una dirección cálida y gastronómica inspirada en una cocina moderna: superficies claras, tonos azules y grisáceos y un acento cobre limitado. Usa Inter para cuerpo y controles y Fraunces para títulos. Fuentes, licencias, iconos y hojas de diseño están versionados bajo `src/Friggy.Web/wwwroot`; la aplicación no solicita recursos visuales a CDN.

Los rangos responsive aprobados son:

| Rango | Composición |
|---|---|
| 360–767 px | Cabecera compacta, drawer y controles apilados |
| 768–1199 px | Drawer y una o dos columnas según el contenido |
| Desde 1200 px | Sidebar fija de 17 rem y contenido fluido |

Al crear o modificar componentes visuales:

* Usar componentes propios con nombres `Friggy*` y clases CSS con prefijo `friggy-`; no introducir bibliotecas UI ni el prefijo genérico `ui-`.
* Consumir colores, espacios, tipografías, radios, sombras, movimiento y breakpoints desde `wwwroot/css/tokens.css`. Los estilos específicos permanecen junto al componente mediante CSS isolation; los patrones compartidos demostrados pueden vivir en una hoja global dedicada.
* Mantener controles con nombre accesible, foco visible, objetivo táctil mínimo de 44×44 px, contraste WCAG 2.2 AA y soporte para `prefers-reduced-motion`.
* Diseñar primero para 360 px y verificar 360×800, 768×1024, 1440×1000 y reflow con zoom al 200 %, sin scroll horizontal de página.
* Servir fuentes, iconos, CSS y JavaScript desde el proyecto. Los componentes no deben llamar a la API ni almacenar reglas de negocio; reciben parámetros y emiten `EventCallback`.

Los E2E visuales capturan Chromium de forma determinista en 360×800, 768×1024 y 1440×1000. Los baseline aprobados son parte del repositorio; las capturas actuales y los diff se guardan bajo `TestResults/visual` y permanecen sin versionar.

Una modificación visual intencionada debe ejecutarse primero para producir `actual` y `diff`. Solo después de revisarlos se aceptan los nuevos baseline mediante:

```powershell
./scripts/update-visual-baselines.ps1 -Accept
```

El gate nunca actualiza baseline automáticamente. El comparador Pixelmatch es tooling de pruebas y no se carga en la aplicación.

Para ejecutar únicamente las reglas arquitectónicas con xUnit v3 sobre MTP:

```powershell
dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-trait "Category=Architecture"
```

## Catálogos iniciales

La aplicación permite administrar ingredientes, unidades, etiquetas de receta y tipos de comida desde Blazor y mediante las rutas `/api/ingredients`, `/api/unit-types`, `/api/recipe-tags` y `/api/meal-types`.

La migración `AddCatalogs` incorpora unidades y tipos de comida iniciales con identificadores estables. Los identificadores públicos se encuentran en `Friggy.Domain.Catalogs.CatalogSeedIds`; incluyen gramo, kilogramo, mililitro, litro, unidad, cucharadita, cucharada, desayuno, comida y cena.

## Futuros cambios

El proyecto recibirá futuras actualizaciones incluyendo lo siguiente:
- Multiusuario con authenticación.
- Inteligencia artificial para la sugerencia de nuevas recetas, recomendaciones a la hora de preparar un plato, ingredientes alternativos, etc. 
- Información nutricional
- Imágenes
- Conversiones entre unidades
- Lista de la compra