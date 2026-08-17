# Plan general post-MVP de Friggy

## Objetivo

Evolucionar el MVP validado hasta un producto local que permita mantener el inventario doméstico, completar comidas con consumo trazable y ofrecer una experiencia visual coherente y responsive, sin mezclar reglas funcionales con detalles de presentación.

El recorrido objetivo es:

> Corregir y navegar el MVP, registrar lotes y movimientos, calcular carencias de una semana, completar una comida consumiendo lotes elegidos, enriquecer la planificación, vincular ingredientes con pasos y presentar todos esos recorridos mediante una interfaz moderna y accesible.

## Principios de alcance

- Se conserva `Domain <- Application <- Infrastructure <- Api`; `Friggy.Web` consume la API por HTTP.
- No se crean proyectos adicionales, microservicios, un bus de mensajes ni una infraestructura CQRS.
- Cada fase entrega un recorrido vertical utilizable y deja verdes las cinco suites.
- El historial de inventario no es event sourcing: el lote mantiene su cantidad actual y los movimientos explican sus cambios.
- La interfaz usa componentes Razor propios, tokens CSS y recursos locales; no incorpora un framework visual ni dependencias de runtime externas.
- La evolución visual conserva rutas, contratos HTTP y comportamiento funcional, y se valida en móvil, tablet y escritorio.

## Secuencia de fases

| Fase | Capacidad | Dependencia principal | Resultado |
|---|---|---|---|
| 7 | [Correcciones del MVP y navegación](completed/phases/07-mvp-corrections-and-navigation.md) | Fase 6 | Recetas eliminables cuando proceda y navegación persistente |
| 8 | [Inventario y finalización de comidas](completed/phases/08-inventory-and-meal-completion.md) | Fase 7 y decisiones de inventario | Lotes, movimientos, raciones, carencias y consumo al completar |
| 9 | [Planificación semanal avanzada](completed/phases/09-advanced-weekly-planning.md) | Fase 8 | Tipos por día, horarios y registro explícito de comidas no realizadas |
| 10 | [Ingredientes asociados a pasos](completed/phases/10-recipe-step-ingredients.md) | Fase 9 | Cada paso identifica qué líneas de ingrediente utiliza |
| 11 | [Sistema visual y experiencia responsive](phases/11-visual-system-and-responsive-ui.md) | Fases 1–10 | Componentes propios, navegación adaptativa y migración visual completa |

No se comienza una fase posterior para compensar una fase anterior incompleta. Los contratos públicos pueden ampliarse, pero no deben romper el recorrido ya publicado sin migración y regresión explícitas.

## Evolución del modelo

- `MealPlanEntry` incorpora raciones y estado de finalización en la Fase 8.
- `InventoryLot` representa una existencia concreta; `InventoryMovement` registra alta, consumo, ajuste y descarte.
- La finalización de una comida y sus consumos se guardan atómicamente.
- La Fase 9 amplía la planificación con selección de tipos por día, hora prevista y estados posteriores distintos de completada.
- La Fase 10 relaciona cada paso con líneas de ingrediente ya pertenecientes a su receta; no duplica las cantidades totales.

## Evolución de la interfaz

- La Fase 11 introduce primero fundamentos visuales y componentes reutilizables y migra después las páginas por prioridad.
- La portada presenta las recetas existentes en tarjetas y mantiene una composición extensible para añadir en el futuro una sección independiente de estadísticas, sin reservar espacios vacíos ni anticipar contratos.
- El diseño se entrega únicamente en modo claro, con tipografías locales, navegación lateral en escritorio y menú deslizable en móvil y tablet.

## Estrategia TDD y verificación

Todo comportamiento funcional o defecto sigue `rojo -> mínimo verde -> refactorización`. Se utilizan:

- xUnit v3 para Domain y Application, con fakes manuales.
- PostgreSQL real con Testcontainers para migraciones, restricciones, concurrencia y consultas.
- `WebApplicationFactory` para contratos HTTP y `ProblemDetails`.
- bUnit para navegación, formularios, estados y accesibilidad.
- Playwright para un recorrido crítico por fase, sin esperas fijas.

Cada migración debe funcionar desde una base vacía y desde el esquema de la fase anterior. El cierre de fase exige `scripts/quality-gate.ps1` en verde.

## Fuera de este plan

- Conversiones o equivalencias entre unidades.
- Ubicaciones físicas de los lotes.
- Lista de la compra automática.
- Inteligencia artificial, imágenes e información nutricional.
- Autenticación, varios usuarios y despliegue público.
- Modo oscuro, una biblioteca UI de terceros o cambios en los contratos de API durante la Fase 11.

## Definición de terminado

Una fase está terminada cuando su comportamiento está cubierto en la capa más baja adecuada, la API y Web exponen el recorrido acordado, las restricciones PostgreSQL están verificadas, los contratos anteriores conservan regresión y el handoff de la fase siguiente no contiene decisiones funcionales ocultas.

## Estado actual

Las fases 1 a 10 están completadas. La siguiente unidad ejecutable es **11.1 — Contrato visual y banco de regresión**.
