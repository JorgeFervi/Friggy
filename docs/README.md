# Documentación de Friggy

Este índice separa la documentación vigente de los documentos históricos. Si quieres usar la aplicación, empieza por las guías de usuario. Si quieres modificarla, empieza por `CONTRIBUTING.md` y las guías de desarrollo.

## Usuarios

* [Instalación y primer arranque](user-guide/getting-started.md)
* [Tu primera semana con Friggy](user-guide/first-steps.md)
* [Recetas y catálogos](user-guide/recipes.md)
* [Planificación diaria](user-guide/daily-planning.md)
* [Plantillas de planes diarios](user-guide/daily-plan-templates.md)
* [Inventario doméstico](user-guide/inventory.md)
* [Lista de la compra](user-guide/shopping-list.md)
* [Persistencia y gestión de datos](user-guide/data-management.md)
* [Resolución de problemas](user-guide/troubleshooting.md)

## Producto

* [Funcionalidades actuales](product/current-capabilities.md)
* [Estado y hoja de ruta](product/roadmap.md)

## Implementaciones con cierre formal pendiente

Las fases 13 a 16 están presentes en el código, pero sus documentos todavía no registran un gate final completo. No se consideran cerradas ni se trasladan a `plans/completed/` hasta disponer de esa evidencia.

* [Fase 13: plantillas de planes diarios](../plans/phases/13-daily-plan-templates.md) y su [guía](../plans/implementation/13-daily-plan-templates-implementation.md)
* [Fase 14: lista de la compra](../plans/phases/14-shopping-list.md) y su [guía](../plans/implementation/14-shopping-list-implementation.md)
* [Fase 15: consolidación de UI y UX](../plans/phases/15-ui-ux-consolidation.md) y su [guía](../plans/implementation/15-ui-ux-consolidation-implementation.md)
* [Fase 16: conversiones y usos contextuales de unidades](plans/phases/16-unit-conversions.md) y su [guía](plans/implementation/16-unit-conversions-implementation.md)

## Desarrollo

* [Contribuir a Friggy](../CONTRIBUTING.md)
* [Entorno local](development/environment.md)
* [Arquitectura](development/architecture.md)
* [Estrategia y ejecución de pruebas](development/testing.md)
* [Persistencia y migraciones](development/database.md)
* [API HTTP](development/api.md)
* [Interfaz, accesibilidad y regresión visual](development/ui-guidelines.md)
* [Recorrido técnico de creación de una receta](development/recipe-creation-walkthrough.md)
* [Recorrido técnico de planificación diaria](development/daily-planning-walkthrough.md)
* [Recorrido técnico de finalización con inventario](development/inventory-completion-walkthrough.md)

## Decisiones arquitectónicas

Los ADR registran por qué se tomó una decisión. Se conservan aunque el producto evolucione; una enmienda o decisión posterior debe quedar indicada explícitamente.

* [ADR 001: ASP.NET Core y Minimal APIs](adr/001-minimal-apis-dotnet.md)
* [ADR 002: PostgreSQL y Docker](adr/002-postgresql-docker.md)
* [ADR 003: Clean Architecture y frontend separado](adr/003-clean-architecture.md)
* [ADR 004: TDD y estrategia de pruebas](adr/004-tdd-and-testing-strategy.md)

## Documentación histórica

Estos documentos explican el origen del producto, pero no son la fuente del comportamiento actual:

* [Ideas iniciales de casos de uso](use-cases/000-general-ideas.md)
* [Definición histórica del MVP](use-cases/001-mvp.md)
* [Planes y fases ejecutadas](../plans/README.md)

## Política de actualización

* Las guías de usuario, producto y desarrollo describen el estado actual.
* Los ADR conservan decisiones y enmiendas con fecha.
* Los documentos de `use-cases` marcados como históricos no definen el alcance vigente.
* Las versiones proceden de `global.json`, `Directory.Packages.props` y `package.json`; evita duplicarlas salvo que sean un requisito de instalación.
* Los resultados y contadores de pruebas pertenecen al gate o al cierre de una fase, no a las guías vivas.
