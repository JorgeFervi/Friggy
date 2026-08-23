# Implementación ejecutable — Fase 16

- **Fase relacionada:** [Conversiones y usos contextuales de unidades](../phases/16-unit-conversions.md)
- **Dependencia ejecutable:** gate verde de catálogos, recetas, inventario, planificación diaria y lista de compra
- **Estado:** implementación presente; PostgreSQL, E2E, visual y gate completo pendientes en un entorno con Docker accesible
- **Skills aplicables:** `architecture`, `modern-csharp`, `entity-framework-core`, `minimal-apis`, `blazor`, `xunit`, `run-tests`
- **Plataforma detectada:** .NET SDK 10.0.302, C# 14, xUnit v3 y Microsoft Testing Platform

La implementación amplía `UnitType`; no crea un agregado, tabla de conversiones, bus ni servicio externo. Domain define compatibilidad y fórmulas puras. Application coordina catálogos y casos de uso. Infrastructure agrega metadatos y traduce consultas. Web continúa consumiendo exclusivamente la API HTTP.

## Semántica cerrada

| Concepto | Regla |
|---|---|
| Base de masa | gramo |
| Base de volumen | mililitro |
| Base de conteo | unidad |
| Sin conversión | compatible solo con el mismo `UnitTypeId` |
| Normalización | `quantity * BaseUnitFactor` |
| Desnormalización | `baseQuantity / BaseUnitFactor` |
| Clave convertible | `(IngredientId, MeasurementDimension)` |
| Clave exacta | `(IngredientId, UnitTypeId)` |
| Unidad de comparación | unidad de compra determinista según cantidad requerida |
| Precisión | `decimal`; sin redondeos intermedios |
| Inventario | unidades de compra; movimientos conservan la unidad del lote |

## 16.1 — Modelo de dominio y conversor puro

Crear `MeasurementConversionTests` y ampliar `UnitTypeTests` para exigir factor positivo, factor 1 para `Unconverted`, equivalencias conocidas, rechazo entre dimensiones y defaults históricos.

Añadir bajo `Friggy.Domain/Catalogs`:

```csharp
public enum MeasurementDimension : short
{
    Unconverted = 0,
    Mass = 1,
    Volume = 2,
    Count = 3,
}
```

Ampliar `UnitType`:

```csharp
public MeasurementDimension MeasurementDimension { get; private set; }
public decimal BaseUnitFactor { get; private set; }
public bool CanUseForCooking { get; private set; }
public bool CanUseForShopping { get; private set; }
```

Conservar `Create(name, symbol)` y `Update(name, symbol)` para no romper consumidores internos. Añadir una sobrecarga completa que delegue en una única validación. `Update(name, symbol)` solo modifica esos campos y nunca restablece metadatos.

Crear `UnitQuantityConverter` puro:

```csharp
public static bool AreCompatible(UnitType source, UnitType target);
public static decimal ToBase(decimal quantity, UnitType unit);
public static decimal FromBase(decimal baseQuantity, UnitType unit);
public static decimal Convert(decimal quantity, UnitType source, UnitType target);
```

Errores estables:

- `unit-type.measurement-dimension.invalid`;
- `unit-type.base-factor.positive`;
- `unit-type.unconverted-factor.invalid`;
- `unit-conversion.incompatible`;
- `unit-conversion.quantity.non-negative`.

No introducir un value object persistido para cantidad: recetas y lotes conservan `decimal + UnitTypeId`.

## 16.2 — Catálogo y contratos HTTP

Ampliar la respuesta:

```csharp
public sealed record UnitTypeResponse(
    Guid Id,
    string Name,
    string Symbol,
    string MeasurementDimension,
    decimal BaseUnitFactor,
    bool CanUseForCooking,
    bool CanUseForShopping);
```

El wire usa `unconverted`, `mass`, `volume` y `count`. No activar globalmente `JsonStringEnumConverter`, porque alteraría otros enums publicados.

```csharp
public sealed record CreateUnitTypeRequest(
    string Name,
    string Symbol,
    string? MeasurementDimension = null,
    decimal? BaseUnitFactor = null,
    bool? CanUseForCooking = null,
    bool? CanUseForShopping = null);

public sealed record UpdateUnitTypeRequest(
    string Name,
    string Symbol,
    string? MeasurementDimension = null,
    decimal? BaseUnitFactor = null,
    bool? CanUseForCooking = null,
    bool? CanUseForShopping = null);
```

En creación, las ausencias significan `unconverted`, 1, true y true. En actualización significan conservar el valor almacenado.

Ampliar `IUnitTypeRepository` para conocer referencias, comprobar que una dimensión conserva una unidad de compra y listar unidades por dimensión.

`UnitTypeService` debe:

1. parsear códigos sin cultura;
2. bloquear cambios de dimensión/factor referenciados con `unit-type.conversion-metadata.in-use` y 409;
3. permitir nombre, símbolo y usos aunque existan referencias;
4. evitar dimensiones con unidades de cocina pero sin unidad de compra mediante `unit-type.shopping-unit.required`;
5. exigir que una unidad `Unconverted` de cocina también sea de compra;
6. mapear siempre todos los metadatos.

Mantener las rutas CRUD actuales. Probar payload antiguo, completo, código inválido, metadatos en uso y OpenAPI.

## 16.3 — Migración PostgreSQL

```powershell
dotnet ef migrations add AddUnitConversionMetadata `
  --project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj `
  --startup-project src/Friggy.Infrastructure/Friggy.Infrastructure.csproj `
  --context FriggyDbContext `
  --output-dir Persistence/Migrations
```

Añadir a `unit_types`:

| Columna | Tipo | Default de migración |
|---|---|---|
| `measurement_dimension` | `smallint` | 0 |
| `base_unit_factor` | `numeric(18,9)` | 1 |
| `can_use_for_cooking` | `boolean` | true |
| `can_use_for_shopping` | `boolean` | true |

Checks:

```text
measurement_dimension BETWEEN 0 AND 3
base_unit_factor > 0
measurement_dimension <> 0 OR base_unit_factor = 1
```

Crear índice no único `(measurement_dimension, can_use_for_shopping, base_unit_factor)`. No imponer unicidad del factor.

Backfill mediante `CatalogSeedIds`:

- Gram/Kilogram → Mass, 1/1000;
- Milliliter/Liter/Teaspoon/Tablespoon → Volume, 1/1000/5/15;
- Unit → Count, 1;
- Teaspoon y Tablespoon con compra deshabilitada;
- unidades personalizadas conservan defaults exactos.

Actualizar configuración y snapshot. Integración debe cubrir base nueva, upgrade con unidad personalizada y referencias, cantidades/movimientos intactos, checks inválidos y round-trip.

## 16.4 — Restricciones en recetas e inventario

### Recetas

Sustituir la comprobación booleana de existencia por una lectura de las unidades solicitadas. En creación, todas deben existir y permitir cocina.

En actualización:

- línea nueva: unidad de cocina;
- línea que cambia `UnitTypeId`: unidad de cocina;
- línea existente: puede conservar unidad histórica deshabilitada;
- inexistente: `recipe.unit-type.not-found`;
- no permitida: `recipe.unit-type.not-allowed-for-cooking`.

Usar `RecipeIngredientRequest.Id` para distinguir líneas retenidas, nuevas y modificadas. Validar antes de mutar o guardar.

### Inventario

`InventoryLotService.CreateAsync` exige `CanUseForShopping`; error `inventory-lot.unit-type.not-allowed-for-shopping`. Operaciones sobre lotes existentes no reaplican el filtro.

### Web

- Receta muestra unidades de cocina e incluye la seleccionada si es histórica, marcada como no disponible para nuevas líneas.
- Alta de inventario muestra solo unidades de compra.
- Catálogo edita dimensión, factor y usos; `Unconverted` fija factor 1.
- Tabla del catálogo muestra dimensión y badges Cocina/Compra.
- La validación Web replica reglas para feedback inmediato, sin sustituir Domain/Application.

## 16.5 — Agregación compartida

Crear bajo `Friggy.Application/Measurements` una política pura:

```csharp
internal readonly record struct MeasurementBucket(
    Guid IngredientId,
    MeasurementDimension Dimension,
    Guid? ExactUnitTypeId);
```

- Dimensión convertible: `ExactUnitTypeId = null`.
- `Unconverted`: conserva el ID exacto.

Selección de salida:

1. filtrar unidades de la dimensión con `CanUseForShopping`;
2. ordenar por factor descendente, nombre normalizado e ID;
3. tomar la primera cuya cantidad requerida convertida sea `>= 1`;
4. si ninguna cumple, usar la de menor factor;
5. para `Unconverted`, devolver la unidad exacta.

Calcular faltante en base con `Max(0, requiredBase - availableBase)` y después convertir requerido, disponible y faltante a la misma unidad.

Tests: combinación g/kg, cucharadas/cucharaditas con stock en litros, dimensiones separadas, stock superior, límites de selección, empate, unidad personalizada y ausencia de unidad de compra.

## 16.6 — Necesidades y finalización

Refactorizar `DailyPlanInventoryService` para cargar unidades una vez y usar la política compartida.

Necesidades:

- normalizar líneas y lotes utilizables;
- agregar por `MeasurementBucket`;
- devolver la unidad de compra elegida;
- conservar filtros de comidas omitidas, agotados y caducidad.

Finalización:

- normalizar necesidad por ingrediente y dimensión;
- convertir cada asignación desde la unidad del lote antes de validar el máximo;
- consumir la cantidad original y mantener movimientos en la unidad del lote;
- convertir cantidad aplicada a base para calcular remanente;
- expresar remanente en unidad de compra;
- conservar transacción, concurrencia, idempotencia y consumo parcial.

Añadir `meal-completion.unit.incompatible` cuando el ingrediente coincide pero la dimensión no. Otro ingrediente conserva `meal-completion.lot.not-required`.

En `DailyPlanDetails`, cargar el catálogo al abrir el diálogo y mostrar lotes del mismo ingrediente con unidad compatible. La API sigue validando autoritativamente.

## 16.7 — Lista de compra y EF Core

Mantener la lista como consulta pura. `ShoppingListReadRepository` ejecuta tres consultas secuenciales en el mismo `DbContext`:

1. demanda agrupada por ingrediente/unidad exacta;
2. stock válido agrupado por ingrediente/unidad exacta;
3. catálogo compacto de unidades.

No usar `Task.WhenAll` con el contexto. Ampliar `ShoppingListSnapshot` con definiciones de unidad y normalizar en `ShoppingListService`.

`ShoppingListItemResponse` conserva su forma, pero unidad y cantidades representan la unidad de compra seleccionada. Una unidad solo de cocina nunca aparece.

Reemplazar el test que separa g/kg por equivalencia convertida. Mantener líneas separadas para dimensiones distintas. La expectativa pasa de dos a tres consultas constantes, sin N+1; revisar SQL y actualizar la medición con la causa documentada.

## 16.8 — Componentes y E2E

No se añaden rutas. Cambios públicos:

- respuestas de unidad añaden dimensión, factor y usos;
- altas/actualizaciones aceptan campos opcionales;
- lista y necesidades pueden devolver una unidad distinta de la almacenada;
- lotes y movimientos no cambian de unidad.

Pruebas bUnit:

- catálogo crea/edita dimensiones y usos;
- `Unconverted` fuerza factor 1;
- receta filtra cocina y conserva selección histórica;
- inventario filtra compra;
- lista y necesidades muestran conversiones;
- diálogo incluye lotes compatibles y excluye dimensiones incompatibles.

E2E:

1. Crear aceite y receta de `1 cda` por ración.
2. Registrar `1 l` de aceite.
3. Planificar varias raciones y comprobar necesidades.
4. Calcular lista y comprobar ml/l, nunca cucharadas.
5. Completar consumiendo del lote en litros.
6. Verificar remanente y movimiento en litros.
7. Deshabilitar cucharada y comprobar que la receta histórica sigue visible, pero no se permite una línea nueva.

Añadir baseline móvil/escritorio del catálogo ampliado y una comparación convertida solo tras revisar `actual` y `diff`.

## 16.9 — Secuencia TDD y comandos

MTP + xUnit v3 sobre SDK 10 exige `--project` y `--filter-class` directo, sin separador `--`.

```powershell
dotnet test --project tests/Friggy.Domain.Tests/Friggy.Domain.Tests.csproj `
  --filter-class "Friggy.Domain.Tests.Catalogs.MeasurementConversionTests"

dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.Catalogs.UnitTypeServiceTests"

dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.Inventory.DailyPlanInventoryServiceTests"

dotnet test --project tests/Friggy.Application.Tests/Friggy.Application.Tests.csproj `
  --filter-class "Friggy.Application.Tests.ShoppingLists.ShoppingListServiceTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Persistence.UnitConversionMigrationTests"

dotnet test --project tests/Friggy.IntegrationTests/Friggy.IntegrationTests.csproj `
  --filter-class "Friggy.IntegrationTests.Api.UnitTypeEndpointTests"

dotnet test --project tests/Friggy.ComponentTests/Friggy.ComponentTests.csproj `
  --filter-class "Friggy.ComponentTests.Catalogs.UnitTypePageTests"

dotnet test --project tests/Friggy.EndToEndTests/Friggy.EndToEndTests.csproj `
  --filter-class "Friggy.EndToEndTests.UnitConversionJourneyTests"

./scripts/quality-gate.ps1
```

Incrementos: Domain; catálogo Application; migración PostgreSQL; restricciones de recetas/inventario; necesidades/compra; finalización; bUnit/E2E/visual; gate.

## 16.10 — Documentación y cierre

Actualizar tras implementar:

- `docs/product/current-capabilities.md` y `roadmap.md`;
- guías de recetas, inventario y lista de compra;
- troubleshooting, retirando la advertencia de coincidencia exacta;
- API e inventory-completion walkthrough;
- README y planes que enumeren conversiones como límite.

No hace falta ADR mientras se mantenga el modelo dimensión/factor. Si se sustituye por grafo de equivalencias o conversiones específicas de ingrediente, detener y registrar la decisión.

Checklist:

- [x] Seeds y unidades personalizadas migran sin reinterpretar cantidades.
- [x] Invariantes Domain y checks PostgreSQL están cubiertos por modelo y migración.
- [x] Peticiones antiguas conservan defaults.
- [x] Recetas/inventario aplican usos sin romper referencias históricas.
- [x] Necesidades, lista y finalización comparten conversión.
- [x] Movimientos permanecen en la unidad del lote.
- [x] Dimensiones y unidades exactas no se mezclan.
- [x] Consultas constantes, cancelación y ausencia de N+1 quedan protegidas por tests.
- [ ] Ejecutar PostgreSQL, E2E, visual y gate completo en un entorno con Docker y revisar los baselines antes de aceptarlos.
