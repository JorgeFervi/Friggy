# Friggy.Infrastructure

> **Estado:** vigente · Consulta también [Persistencia y migraciones](../../docs/development/database.md).

La capa Friggy.Infrastructure contiene las implementaciones técnicas que
permiten a Friggy persistir y consultar los datos del dominio. Configura
Entity Framework Core con PostgreSQL, implementa los contratos definidos por
la capa de aplicación y mantiene el historial de cambios de la base de datos.

Esta capa depende de Friggy.Domain y Friggy.Application. La API la utiliza
como parte del composition root mediante [DependencyInjection](DependencyInjection.cs).

## Organización

| Carpeta | Contenido |
|---|---|
| [Persistence](Persistence) | Contexto de datos, factoría, unidad de trabajo, configuraciones, repositorios y migraciones. |
| [Persistence/Configurations](Persistence/Configurations) | Configuración de tablas, claves, relaciones, restricciones e índices de las entidades del dominio. |
| [Persistence/Repositories](Persistence/Repositories) | Implementaciones de los contratos de persistencia de la capa de aplicación. |
| [Persistence/Migrations](Persistence/Migrations) | Historial versionado del esquema de PostgreSQL generado por Entity Framework Core. |

## Elementos compartidos

| Tipo | Representación |
|---|---|
| [AssemblyMarker](AssemblyMarker.cs) | Clase marcador que permite obtener la asamblea de la capa de infraestructura. |
| [DependencyInjection](DependencyInjection.cs) | Clase que registra el contexto de datos, PostgreSQL y todos los repositorios de infraestructura. |

## Persistencia principal

| Tipo | Representación |
|---|---|
| [FriggyDbContext](Persistence/FriggyDbContext.cs) | Contexto de Entity Framework Core que expone los conjuntos persistidos y aplica las configuraciones del modelo. |
| [FriggyDbContextFactory](Persistence/FriggyDbContextFactory.cs) | Factoría que crea el contexto para las herramientas de Entity Framework Core en tiempo de diseño. |
| [InventoryUnitOfWork](Persistence/InventoryUnitOfWork.cs) | Unidad de trabajo que persiste operaciones de inventario y traduce conflictos de concurrencia a errores de aplicación. |

## Configuraciones de Entity Framework Core

| Tipo | Representación |
|---|---|
| [CatalogConfiguration](Persistence/Configurations/CatalogConfiguration.cs) | Configuración auxiliar del value object CatalogName, incluidas sus columnas e índice único normalizado. |
| [IngredientConfiguration](Persistence/Configurations/IngredientConfiguration.cs) | Configuración de la tabla y del nombre de los ingredientes. |
| [UnitTypeConfiguration](Persistence/Configurations/UnitTypeConfiguration.cs) | Configuración de unidades de medida, símbolos y nombres. |
| [MealTypeConfiguration](Persistence/Configurations/MealTypeConfiguration.cs) | Configuración de tipos de comida, nombres y orden de presentación. |
| [RecipeTagConfiguration](Persistence/Configurations/RecipeTagConfiguration.cs) | Configuración de etiquetas de receta y sus nombres. |
| [RecipeConfiguration](Persistence/Configurations/RecipeConfiguration.cs) | Configuración de recetas, tiempo estimado, nombre y colecciones agregadas. |
| [RecipeIngredientConfiguration](Persistence/Configurations/RecipeIngredientConfiguration.cs) | Configuración de las líneas de ingrediente, sus cantidades, posiciones y referencias. |
| [RecipeStepConfiguration](Persistence/Configurations/RecipeStepConfiguration.cs) | Configuración de los pasos de receta, sus tiempos, posiciones y asociaciones. |
| [RecipeTagLinkConfiguration](Persistence/Configurations/RecipeTagLinkConfiguration.cs) | Configuración de la asociación entre recetas y etiquetas. |
| [RecipeMealTypeLinkConfiguration](Persistence/Configurations/RecipeMealTypeLinkConfiguration.cs) | Configuración de la asociación entre recetas y tipos de comida. |
| [RecipeStepIngredientLinkConfiguration](Persistence/Configurations/RecipeStepIngredientLinkConfiguration.cs) | Configuración de la asociación entre pasos y líneas de ingrediente. |
| [WeeklyPlanConfiguration](Persistence/Configurations/WeeklyPlanConfiguration.cs) | Configuración de planes semanales, fechas, descripción y colecciones. |
| [MealPlanEntryConfiguration](Persistence/Configurations/MealPlanEntryConfiguration.cs) | Configuración de asignaciones de comidas, estados, restricciones y relaciones. |
| [MealPlanSlotConfiguration](Persistence/Configurations/MealPlanSlotConfiguration.cs) | Configuración de huecos de comida, orden, horario y claves alternativas. |
| [InventoryLotConfiguration](Persistence/Configurations/InventoryLotConfiguration.cs) | Configuración de lotes de inventario, cantidades, caducidad, concurrencia y movimientos. |
| [InventoryMovementConfiguration](Persistence/Configurations/InventoryMovementConfiguration.cs) | Configuración de movimientos, variaciones, cantidades resultantes y relaciones. |

## Repositorios

### Catálogos

| Tipo | Representación |
|---|---|
| [IngredientRepository](Persistence/Repositories/IngredientRepository.cs) | Implementación EF Core de la persistencia y consulta de ingredientes. |
| [UnitTypeRepository](Persistence/Repositories/UnitTypeRepository.cs) | Implementación EF Core de la persistencia y consulta de unidades de medida. |
| [MealTypeRepository](Persistence/Repositories/MealTypeRepository.cs) | Implementación EF Core de la persistencia y consulta de tipos de comida. |
| [RecipeTagRepository](Persistence/Repositories/RecipeTagRepository.cs) | Implementación EF Core de la persistencia y consulta de etiquetas de receta. |

### Recetas

| Tipo | Representación |
|---|---|
| [RecipeRepository](Persistence/Repositories/RecipeRepository.cs) | Implementación EF Core que carga y persiste recetas con sus ingredientes, pasos y asociaciones. |
| [RecipeCatalogRepository](Persistence/Repositories/RecipeCatalogRepository.cs) | Implementación EF Core que comprueba la existencia de referencias de catálogo usadas por las recetas. |

### Inventario

| Tipo | Representación |
|---|---|
| [InventoryLotRepository](Persistence/Repositories/InventoryLotRepository.cs) | Implementación EF Core que consulta y persiste lotes junto con sus movimientos y gestiona la concurrencia. |
| [InventoryReferenceRepository](Persistence/Repositories/InventoryReferenceRepository.cs) | Implementación EF Core que consulta ingredientes y unidades de medida para las operaciones de inventario. |

### Planificación semanal

| Tipo | Representación |
|---|---|
| [WeeklyPlanRepository](Persistence/Repositories/WeeklyPlanRepository.cs) | Implementación EF Core que carga y persiste planes, asignaciones y huecos, incluyendo sus reordenaciones. |
| [WeeklyPlanReferenceRepository](Persistence/Repositories/WeeklyPlanReferenceRepository.cs) | Implementación EF Core que consulta recetas, tiempos estimados y tipos de comida para los planes. |

## Migraciones y modelo

Las migraciones son clases generadas por Entity Framework Core que representan
la evolución versionada del esquema. Cada migración tiene un archivo principal
y un archivo Designer asociado; el índice enlaza el archivo principal.

| Migración | Representación |
|---|---|
| [InitialInfrastructure](Persistence/Migrations/20260806074158_InitialInfrastructure.cs) | Crea la estructura inicial de infraestructura. |
| [AddCatalogs](Persistence/Migrations/20260806111514_AddCatalogs.cs) | Añade las tablas y datos iniciales de los catálogos. |
| [AddRecipes](Persistence/Migrations/20260809121949_AddRecipes.cs) | Añade las tablas y relaciones de las recetas. |
| [AddWeeklyPlans](Persistence/Migrations/20260809201541_AddWeeklyPlans.cs) | Añade las tablas de planes semanales y sus asignaciones. |
| [AddInventoryAndMealCompletion](Persistence/Migrations/20260812063108_AddInventoryAndMealCompletion.cs) | Añade inventario, movimientos y datos necesarios para completar comidas. |
| [AddDailyMealPlanSlots](Persistence/Migrations/20260812101526_AddDailyMealPlanSlots.cs) | Añade los huecos diarios de los planes semanales. |
| [AddMealPlanSlotSchedule](Persistence/Migrations/20260812103001_AddMealPlanSlotSchedule.cs) | Añade el horario previsto de los huecos de comida. |
| [AddMealPlanEntrySkippedState](Persistence/Migrations/20260812104103_AddMealPlanEntrySkippedState.cs) | Añade el estado de comida omitida y sus datos asociados. |
| [AddRecipeStepIngredients](Persistence/Migrations/20260813083730_AddRecipeStepIngredients.cs) | Añade las asociaciones entre pasos y líneas de ingrediente. |
| [DeferRecipeIngredientOrderUniqueness](Persistence/Migrations/20260813085711_DeferRecipeIngredientOrderUniqueness.cs) | Ajusta la gestión de unicidad del orden de las líneas de ingrediente. |
| [FriggyDbContextModelSnapshot](Persistence/Migrations/FriggyDbContextModelSnapshot.cs) | Representa el modelo actual conocido por Entity Framework Core para calcular futuras migraciones. |

## Flujo de persistencia

1. La API registra esta capa mediante DependencyInjection.
2. FriggyDbContext configura el modelo aplicando las configuraciones de
   Persistence/Configurations.
3. Los servicios de aplicación utilizan los repositorios mediante las
   interfaces definidas en Friggy.Application.
4. Los repositorios consultan o modifican PostgreSQL a través de Entity
   Framework Core.
5. Las migraciones mantienen versionado el esquema de la base de datos.

La infraestructura no contiene reglas de negocio; adapta la persistencia y
los servicios técnicos a los contratos que necesita la aplicación.
