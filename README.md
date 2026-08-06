# Friggy

Friggy es una aplicación para organizar y planificar tus comidas semanales de forma sencilla.

La aplicación te permite registrar los alimentos que tienes en casa, guardar tus propias recetas, definir distintos tipos de comida y organizar tu menú mediante un calendario semanal.

Además, Friggy puede conectar tus datos con un sistema de inteligencia artificial para ayudarte a:

* Crear una planificación semanal adaptada a tus preferencias.
* Aprovechar los alimentos que ya tienes disponibles.
* Analizar tus hábitos alimentarios y ofrecerte recomendaciones de mejora.
* Descubrir nuevos platos y recetas basados en tus gustos.

El objetivo de Friggy es facilitar la planificación de las comidas, reducir el desperdicio de alimentos y ayudarte a mantener una alimentación más variada y organizada.

## Tecnologías previstas para el MVP

* **Backend:** ASP.NET Core con Minimal APIs sobre .NET 10.
* **Interfaz:** Blazor Web App con interactividad en servidor.
* **Persistencia:** PostgreSQL ejecutado localmente mediante Docker Compose.
* **Acceso a datos:** Entity Framework Core con migraciones versionadas.

La definición funcional, el alcance y el calendario de desarrollo se encuentran en [Definición del MVP](docs/use-cases/001-mvp.md). Las decisiones arquitectónicas se documentan en [docs/adr](docs/adr).

## Ejecución local

Desde PowerShell, ejecutar los scripts en este orden:

```powershell
./scripts/setup.ps1
./scripts/start.ps1
```

`setup.ps1` comprueba .NET y Docker, arranca PostgreSQL, restaura herramientas y paquetes, aplica las migraciones e instala Chromium para Playwright. Puede ejecutarse de nuevo sin eliminar datos ni recrear la configuración local.

`start.ps1` inicia la API en `http://localhost:5292` y la aplicación Web en `http://localhost:5179`. Si ya están disponibles, no crea procesos duplicados. Los logs locales se guardan bajo `.friggy/logs`, que no se versiona.

Para compilar una vez y ejecutar todas las suites en orden:

```powershell
./scripts/test.ps1
```

El script usa la configuración `Release` para no interferir con los procesos locales `Debug` iniciados por `start.ps1`, y termina inmediatamente si falla cualquier comando o suite.
