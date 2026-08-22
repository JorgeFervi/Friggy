# Friggy.Application

> **Estado:** vigente · Consulta también [Arquitectura para contribuidores](../../docs/development/architecture.md).

La capa Friggy.Application coordina los casos de uso de Friggy. Recibe
peticiones mediante sus DTOs, aplica el flujo de aplicación sobre las
entidades del dominio y utiliza contratos de repositorio para consultar o
persistir información.

Esta capa depende de Friggy.Domain, pero no conoce los detalles de
infraestructura. Las implementaciones de sus interfaces se encuentran en la
capa de infraestructura y los servicios se registran mediante
[DependencyInjection](DependencyInjection.cs).

## Organización

| Carpeta | Contenido |
|---|---|
| [Catalogs](Catalogs) | Casos de uso y contratos para ingredientes, tipos de comida, etiquetas y unidades de medida. |
| [Recipes](Recipes) | Casos de uso, DTOs y contratos relacionados con recetas. |
| [Inventory](Inventory) | Casos de uso, DTOs y contratos relacionados con lotes y operaciones de inventario. |
| [DailyPlans](DailyPlans) | Casos de uso, DTOs y contratos relacionados con la planificación por fecha. |

## Elementos compartidos

| Tipo | Representación |
|---|---|
| [AssemblyMarker](AssemblyMarker.cs) | Clase marcador que permite obtener la asamblea de la capa de aplicación. |
| [DependencyInjection](DependencyInjection.cs) | Clase que registra los servicios de aplicación y el proveedor de tiempo en el contenedor de dependencias. |

## Catálogos

### Errores y clasificación

| Tipo | Representación |
|---|---|
| [CatalogException](Catalogs/CatalogExceptions.cs) | Excepción base para los fallos producidos al ejecutar casos de uso de catálogos. |
| [CatalogConflictException](Catalogs/CatalogExceptions.cs) | Excepción que indica un conflicto al modificar un elemento de catálogo. |
| [CatalogNotFoundException](Catalogs/CatalogExceptions.cs) | Excepción que indica que no se encontró un elemento de catálogo. |
| [CatalogFailureKind](Catalogs/CatalogExceptions.cs) | Enumeración de fallos de validación, ausencia de datos o conflicto. |
| [CatalogFailure](Catalogs/CatalogExceptions.cs) | Resultado clasificado de un fallo de catálogo con su tipo y código. |
| [CatalogFailureClassifier](Catalogs/CatalogExceptions.cs) | Clase que traduce excepciones de dominio y aplicación a fallos de catálogo. |

### Ingredientes

| Tipo | Representación |
|---|---|
| [IngredientService](Catalogs/Ingredients/Services/IngredientService.cs) | Servicio que lista, consulta, crea, actualiza y elimina ingredientes. |
| [IIngredientRepository](Catalogs/Ingredients/Interfaces/IIngredientRepository.cs) | Contrato de persistencia de ingredientes y de comprobación de nombres normalizados. |
| [CreateIngredientRequest](Catalogs/Ingredients/Dtos/CreateIngredientRequest.cs) | Datos de entrada para crear un ingrediente. |
| [UpdateIngredientRequest](Catalogs/Ingredients/Dtos/UpdateIngredientRequest.cs) | Datos de entrada para actualizar un ingrediente. |
| [IngredientResponse](Catalogs/Ingredients/Dtos/IngredientResponse.cs) | Datos de salida de un ingrediente. |

### Tipos de comida

| Tipo | Representación |
|---|---|
| [MealTypeService](Catalogs/MealTypes/Services/MealTypeService.cs) | Servicio que lista, consulta, crea, actualiza y elimina tipos de comida. |
| [IMealTypeRepository](Catalogs/MealTypes/Interfaces/IMealTypeRepository.cs) | Contrato de persistencia de tipos de comida y de comprobación de nombres normalizados. |
| [CreateMealTypeRequest](Catalogs/MealTypes/Dtos/CreateMealTypeRequest.cs) | Datos de entrada para crear un tipo de comida. |
| [UpdateMealTypeRequest](Catalogs/MealTypes/Dtos/UpdateMealTypeRequest.cs) | Datos de entrada para actualizar un tipo de comida. |
| [MealTypeResponse](Catalogs/MealTypes/Dtos/MealTypeResponse.cs) | Datos de salida de un tipo de comida, incluido su orden. |

### Etiquetas de receta

| Tipo | Representación |
|---|---|
| [RecipeTagService](Catalogs/RecipeTags/Services/RecipeTagService.cs) | Servicio que lista, consulta, crea, actualiza y elimina etiquetas de receta. |
| [IRecipeTagRepository](Catalogs/RecipeTags/Interfaces/IRecipeTagRepository.cs) | Contrato de persistencia de etiquetas y de comprobación de nombres normalizados. |
| [CreateRecipeTagRequest](Catalogs/RecipeTags/Dtos/CreateRecipeTagRequest.cs) | Datos de entrada para crear una etiqueta de receta. |
| [UpdateRecipeTagRequest](Catalogs/RecipeTags/Dtos/UpdateRecipeTagRequest.cs) | Datos de entrada para actualizar una etiqueta de receta. |
| [RecipeTagResponse](Catalogs/RecipeTags/Dtos/RecipeTagResponse.cs) | Datos de salida de una etiqueta de receta. |

### Unidades de medida

| Tipo | Representación |
|---|---|
| [UnitTypeService](Catalogs/UnitTypes/Services/UnitTypeService.cs) | Servicio que lista, consulta, crea, actualiza y elimina unidades de medida. |
| [IUnitTypeRepository](Catalogs/UnitTypes/Interfaces/IUnitTypeRepository.cs) | Contrato de persistencia de unidades y de comprobación de nombres normalizados. |
| [CreateUnitTypeRequest](Catalogs/UnitTypes/Dtos/CreateUnitTypeRequest.cs) | Datos de entrada para crear una unidad de medida. |
| [UpdateUnitTypeRequest](Catalogs/UnitTypes/Dtos/UpdateUnitTypeRequest.cs) | Datos de entrada para actualizar una unidad de medida. |
| [UnitTypeResponse](Catalogs/UnitTypes/Dtos/UnitTypeResponse.cs) | Datos de salida de una unidad de medida, incluido su símbolo. |

## Recetas

### Servicios y contratos

| Tipo | Representación |
|---|---|
| [RecipeService](Recipes/Services/RecipeService.cs) | Servicio que coordina la creación, consulta, actualización y eliminación de recetas, incluida la comprobación de sus referencias de catálogo. |
| [IRecipeRepository](Recipes/Interfaces/IRecipeRepository.cs) | Contrato de persistencia de recetas y de consulta de resúmenes y nombres normalizados. |
| [IRecipeCatalogRepository](Recipes/Interfaces/IRecipeCatalogRepository.cs) | Contrato para comprobar que existan los ingredientes, unidades, etiquetas y tipos de comida usados por una receta. |

### DTOs

| Tipo | Representación |
|---|---|
| [CreateRecipeRequest](Recipes/Dtos/CreateRecipeRequest.cs) | Datos de entrada para crear una receta completa. |
| [UpdateRecipeRequest](Recipes/Dtos/UpdateRecipeRequest.cs) | Datos de entrada para actualizar una receta completa. |
| [RecipeIngredientRequest](Recipes/Dtos/RecipeIngredientRequest.cs) | Datos de una línea de ingrediente recibidos al crear o actualizar una receta. |
| [RecipeStepRequest](Recipes/Dtos/RecipeStepRequest.cs) | Datos de un paso recibidos al crear o actualizar una receta. |
| [RecipeListItemResponse](Recipes/Dtos/RecipeListItemResponse.cs) | Resumen de una receta para listados. |
| [RecipeResponse](Recipes/Dtos/RecipeResponse.cs) | Datos completos de respuesta de una receta. |
| [RecipeIngredientResponse](Recipes/Dtos/RecipeIngredientResponse.cs) | Datos de salida de una línea de ingrediente. |
| [RecipeStepResponse](Recipes/Dtos/RecipeStepResponse.cs) | Datos de salida de un paso de preparación. |

### Errores y clasificación

| Tipo | Representación |
|---|---|
| [RecipeApplicationException](Recipes/Exceptions/RecipeApplicationException.cs) | Excepción base para los fallos producidos al ejecutar casos de uso de recetas. |
| [RecipeNameConflictException](Recipes/Exceptions/RecipeNameConflictException.cs) | Excepción que indica que el nombre de una receta ya está en uso. |
| [RecipeNotFoundException](Recipes/Exceptions/RecipeNotFoundException.cs) | Excepción que indica que no se encontró una receta. |
| [RecipeReferenceNotFoundException](Recipes/Exceptions/RecipeReferenceNotFoundException.cs) | Excepción que indica que falta una referencia de catálogo necesaria para una receta. |
| [RecipeFailureKind](Recipes/Exceptions/RecipeFailureClassifier.cs) | Enumeración de fallos por ausencia de datos o conflicto. |
| [RecipeFailure](Recipes/Exceptions/RecipeFailureClassifier.cs) | Resultado clasificado de un fallo de receta con su tipo y código. |
| [RecipeFailureClassifier](Recipes/Exceptions/RecipeFailureClassifier.cs) | Clase que traduce excepciones de receta y del dominio a fallos de aplicación. |

## Inventario

### Servicios y contratos

| Tipo | Representación |
|---|---|
| [InventoryLotService](Inventory/Services/InventoryLotService.cs) | Servicio que lista, consulta, crea, corrige, consume, descarta y ajusta lotes de inventario. |
| [DailyPlanInventoryService](Inventory/Services/DailyPlanInventoryService.cs) | Servicio que calcula las necesidades de una fecha y coordina el consumo al completar una comida. |
| [IInventoryLotRepository](Inventory/Interfaces/IInventoryLotRepository.cs) | Contrato de persistencia y consulta de lotes de inventario. |
| [IInventoryReferenceRepository](Inventory/Interfaces/IInventoryReferenceRepository.cs) | Contrato para consultar ingredientes y unidades necesarios para mostrar o validar lotes. |
| [IInventoryUnitOfWork](Inventory/Interfaces/IInventoryUnitOfWork.cs) | Contrato para persistir una operación completa de inventario. |

### DTOs

| Tipo | Representación |
|---|---|
| [CreateInventoryLotRequest](Inventory/Dtos/CreateInventoryLotRequest.cs) | Datos de entrada para crear un lote de inventario. |
| [CorrectInventoryExpirationRequest](Inventory/Dtos/CorrectInventoryExpirationRequest.cs) | Datos de entrada para corregir la caducidad de un lote. |
| [AdjustInventoryLotRequest](Inventory/Dtos/AdjustInventoryLotRequest.cs) | Datos de entrada para ajustar la cantidad real de un lote. |
| [InventoryQuantityRequest](Inventory/Dtos/InventoryQuantityRequest.cs) | Cantidad de entrada para consumir o descartar existencias. |
| [InventoryLotAllocationRequest](Inventory/Dtos/InventoryLotAllocationRequest.cs) | Cantidad de un lote que se propone consumir al completar una comida. |
| [CompleteMealRequest](Inventory/Dtos/CompleteMealRequest.cs) | Distribución de lotes y cantidades que se usará para completar una comida. |
| [InventoryLotResponse](Inventory/Dtos/InventoryLotResponse.cs) | Datos de salida de un lote, sus referencias y sus movimientos. |
| [InventoryMovementResponse](Inventory/Dtos/InventoryMovementResponse.cs) | Datos de salida de un movimiento de inventario. |
| [InventoryOperationResponse](Inventory/Dtos/InventoryOperationResponse.cs) | Resultado de consumir o descartar existencias junto con el lote actualizado. |
| [InventoryRequirementResponse](Inventory/Dtos/InventoryRequirementResponse.cs) | Necesidad calculada de un ingrediente, comparada con la cantidad disponible. |
| [MealLotConsumptionResponse](Inventory/Dtos/MealLotConsumptionResponse.cs) | Cantidad aplicada y no aplicada sobre un lote al completar una comida. |
| [MealCompletionRemainderResponse](Inventory/Dtos/MealCompletionRemainderResponse.cs) | Cantidad de un ingrediente que permanece pendiente al completar una comida. |
| [MealCompletionResponse](Inventory/Dtos/MealCompletionResponse.cs) | Resultado de completar una comida, con consumos y necesidades pendientes. |
| [InventoryMovementKind](Inventory/Dtos/InventoryMovementKind.cs) | Enumeración de los tipos de movimiento expuestos por la aplicación. |

### Errores y clasificación

| Tipo | Representación |
|---|---|
| [InventoryApplicationException](Inventory/Exceptions/InventoryApplicationException.cs) | Excepción base para los fallos producidos al ejecutar casos de uso de inventario. |
| [InventoryConflictException](Inventory/Exceptions/InventoryConflictException.cs) | Excepción que indica un conflicto al operar sobre existencias. |
| [InventoryNotFoundException](Inventory/Exceptions/InventoryNotFoundException.cs) | Excepción que indica que no se encontró un lote o recurso de inventario. |
| [InventoryReferenceNotFoundException](Inventory/Exceptions/InventoryReferenceNotFoundException.cs) | Excepción que indica que falta una referencia necesaria para operar con inventario. |
| [InventoryFailureKind](Inventory/Exceptions/InventoryFailureClassifier.cs) | Enumeración de fallos por ausencia de datos o conflicto. |
| [InventoryFailure](Inventory/Exceptions/InventoryFailureClassifier.cs) | Resultado clasificado de un fallo de inventario con su tipo y código. |
| [InventoryFailureClassifier](Inventory/Exceptions/InventoryFailureClassifier.cs) | Clase que traduce excepciones de inventario a fallos reconocibles por la API. |

## Planificación diaria

### Servicios y contratos

| Tipo | Representación |
|---|---|
| [DailyPlanService](DailyPlans/Services/DailyPlanService.cs) | Servicio que gestiona planes por fecha, intervalos inclusivos, recetas y huecos. |
| [IDailyPlanRepository](DailyPlans/Interfaces/IDailyPlanRepository.cs) | Contrato de persistencia por fecha e intervalo de planes diarios. |
| [IDailyPlanReferenceRepository](DailyPlans/Interfaces/IDailyPlanReferenceRepository.cs) | Contrato para comprobar referencias y consultar tiempos de recetas en lote. |

### DTOs

| Tipo | Representación |
|---|---|
| [CreateDailyPlanRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Fecha para crear un plan diario. |
| [DailyPlanRangeResponse](DailyPlans/Dtos/DailyPlanDtos.cs) | Planes existentes dentro de un intervalo inclusivo. |
| [DailyPlanResponse](DailyPlans/Dtos/DailyPlanDtos.cs) | Plan completo de una fecha. |
| [DailyPlanMealResponse](DailyPlans/Dtos/DailyPlanDtos.cs) | Hueco, receta, comensales, horario y estado de una comida. |
| [SetMealPlanEntryRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Receta y comensales que se asignan a una comida. |
| [SkipMealPlanEntryRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Motivo y alternativa opcional al omitir una comida. |
| [AddMealPlanSlotRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Tipo de comida que se añade al día. |
| [SetMealPlanSlotTimeRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Hora prevista de un hueco. |
| [ReorderMealPlanSlotsRequest](DailyPlans/Dtos/DailyPlanDtos.cs) | Nuevo orden completo de los huecos. |

### Errores y clasificación

| Tipo | Representación |
|---|---|
| [DailyPlanApplicationException](DailyPlans/Exceptions/DailyPlanExceptions.cs) | Excepción base para los fallos de planificación diaria. |
| [DailyPlanDateConflictException](DailyPlans/Exceptions/DailyPlanExceptions.cs) | Conflicto al intentar crear un segundo plan para la misma fecha. |
| [DailyPlanNotFoundException](DailyPlans/Exceptions/DailyPlanExceptions.cs) | Indica que no existe plan para la fecha solicitada. |
| [DailyPlanReferenceNotFoundException](DailyPlans/Exceptions/DailyPlanExceptions.cs) | Indica que falta una receta o tipo de comida referenciado. |
| [DailyPlanFailureClassifier](DailyPlans/Exceptions/DailyPlanExceptions.cs) | Traduce excepciones diarias a fallos reconocibles por la API. |

## Flujo general

1. La API recibe una petición y la transforma en uno de los DTOs de entrada.
2. El servicio de aplicación coordina validaciones, referencias y operaciones del
   dominio.
3. Los contratos de repositorio y unidad de trabajo permiten consultar o
   persistir sin acoplar esta capa a una tecnología concreta.
4. El servicio devuelve un DTO de respuesta o una excepción clasificada para que
   la API la traduzca a su respuesta HTTP.

La capa de aplicación no contiene configuraciones de base de datos ni
implementaciones de repositorios; esas responsabilidades pertenecen a
Friggy.Infrastructure.
