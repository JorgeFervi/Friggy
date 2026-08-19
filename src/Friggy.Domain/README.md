# Friggy.Domain

La capa `Friggy.Domain` contiene las reglas de negocio y los modelos propios
del dominio de Friggy. No depende de la infraestructura, de la API ni de la
interfaz web. Sus clases protegen las invariantes de catálogos, recetas,
inventario y planificación semanal.

## Organización

| Carpeta | Contenido |
|---|---|
| [`Catalogs`](Catalogs) | Catálogos, nombres de catálogo y excepciones de validación. |
| [`Inventory`](Inventory) | Lotes, movimientos y resultados de operaciones de inventario. |
| [`Recipes`](Recipes) | Recetas, ingredientes, pasos y asociaciones de recetas. |
| [`WeeklyPlans`](WeeklyPlans) | Planes semanales, huecos, asignaciones y estados de comidas. |

## Catálogos

| Tipo | Representación |
|---|---|
| [`CatalogName`](Catalogs/CatalogName.cs) | Value object que representa un nombre o una descripción de catálogo. Conserva también su valor normalizado. |
| [`CatalogSeedIds`](Catalogs/CatalogSeedIds.cs) | Contenedor de códigos de identificación usados por los datos iniciales de unidades y tipos de comida. |
| [`DomainValidationException`](Catalogs/DomainValidationException.cs) | Excepción que indica que se ha incumplido una regla de negocio del dominio; incluye un código de error. |
| [`Ingredient`](Catalogs/Ingredient.cs) | Ingrediente que puede utilizarse en uno o varios pasos de una receta. |
| [`MealType`](Catalogs/MealType.cs) | Tipo de comida que permite clasificar recetas y organizar comidas dentro de un plan. |
| [`RecipeTag`](Catalogs/RecipeTag.cs) | Etiqueta que puede asociarse a una receta. |
| [`UnitType`](Catalogs/UnitType.cs) | Unidad de medida utilizada para expresar cantidades de ingredientes, junto con su símbolo. |

## Inventario

| Tipo | Representación |
|---|---|
| [`InventoryLot`](Inventory/InventoryLot.cs) | Lote de existencias de un ingrediente y una fecha de caducidad concretas. Mantiene la cantidad disponible y sus movimientos. |
| [`InventoryMovement`](Inventory/InventoryMovement.cs) | Movimiento que registra una variación de la cantidad de un lote y la cantidad resultante. |
| [`InventoryMovementType`](Inventory/InventoryMovementType.cs) | Enumeración de los tipos de movimiento: entrada inicial, consumo, ajuste y descarte. |
| [`InventoryOperationResult`](Inventory/InventoryOperationResult.cs) | Resultado de una operación de reducción de existencias, con la cantidad aplicada y la que no pudo aplicarse. |

## Recetas

| Tipo | Representación |
|---|---|
| [`Recipe`](Recipes/Recipe.cs) | Entidad principal de una receta que relaciona todas las demas clases que representan los pasos a seguir de una receta, los ingredientes, etiquetas y tipos de comida. Gestiona su nombre, tiempo estimado, ingredientes, pasos, etiquetas y tipos de comida. |
| [`RecipeConflictException`](Recipes/RecipeConflictException.cs) | Excepción que indica un conflicto de identidad o unicidad dentro de una receta. |
| [`RecipeIngredient`](Recipes/RecipeIngredient.cs) | Línea de ingrediente de una receta, con ingrediente, unidad de medida, cantidad y posición. |
| [`RecipeMealTypeLink`](Recipes/RecipeMealTypeLink.cs) | Asociación entre una receta y un tipo de comida. |
| [`RecipeStep`](Recipes/RecipeStep.cs) | Paso de preparación de una receta, con descripción, tiempo estimado, posición y sus ingredientes asociados. |
| [`RecipeStepIngredientLink`](Recipes/RecipeStepIngredientLink.cs) | Asociación entre un paso y una línea de ingrediente de la misma receta. |
| [`RecipeTagLink`](Recipes/RecipeTagLink.cs) | Asociación entre una receta y una etiqueta. |

## Planificación semanal

| Tipo | Representación |
|---|---|
| [`WeeklyPlan`](WeeklyPlans/WeeklyPlan.cs) | Entidad principal que representa una semana completa y gestiona sus fechas, huecos y asignaciones de recetas. |
| [`MealPlanSlot`](WeeklyPlans/MealPlanSlot.cs) | Hueco de comida disponible en un día del plan, con tipo de comida, orden y hora prevista de preparación. |
| [`MealPlanEntry`](WeeklyPlans/MealPlanEntry.cs) | Receta asignada a un tipo de comida y un día del plan, con sus raciones y estado de ejecución. |
| [`MealPlanEntryStatus`](WeeklyPlans/MealPlanEntryStatus.cs) | Enumeración que indica si una asignación está planificada, completada u omitida. |

## Relaciones principales

- `Recipe` contiene `RecipeIngredient` y `RecipeStep`, y se puede clasificar usando sus asociaciones con `RecipeTagLink` y `RecipeMealTypeLink`.
- `RecipeStep` relaciona sus pasos con las líneas de ingredientes mediante `RecipeStepIngredientLink`.
- `InventoryLot` registra cada cambio de existencias mediante `InventoryMovement`.
- `WeeklyPlan` contiene `MealPlanSlot` y `MealPlanEntry` para representar la estructura del plan semanal (si se hacen 3 comidas o 5) y el estado por cada comida para indicar si se ha cumplido con el objetivo de la planificación de la semana.
- `CatalogName` se utiliza como value object para mantener válidos los nombres de las entidades de catálogo.

Las clases del dominio crean y modifican sus entidades mediante métodos que validan
las reglas de negocio. Las capas superiores pueden traducir o presentar los errores mediante 
[`DomainValidationException`](Catalogs/DomainValidationException.cs) y
[`RecipeConflictException`](Recipes/RecipeConflictException.cs).
