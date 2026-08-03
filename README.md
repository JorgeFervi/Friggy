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

## Ejecución local prevista

El primer scaffold del proyecto incorporará los siguientes scripts de PowerShell:

* `scripts/setup.ps1`: comprobará los requisitos, preparará la configuración local, arrancará PostgreSQL, restaurará las dependencias y aplicará las migraciones.
* `scripts/start.ps1`: arrancará PostgreSQL si fuese necesario e iniciará la aplicación.

Hasta que se cree el scaffold de la aplicación, estos comandos forman parte del plan de implementación y todavía no están disponibles.
