# Implementación ejecutable — Fase 6

- **Fase relacionada:** [Fase 6 — Estabilización y piloto](../phases/06-stabilization-and-pilot.md)
- **Agentes instalados:** `dotnet-router`, `dotnet-build`, `dotnet-review`, `test-quality-auditor`, `code-testing-planner`
- **Skills instaladas:** `quality-ci`, `code-review`, `run-tests`, `assertion-quality`, `test-anti-patterns`, `coverage-analysis`, `entity-framework-core`, `project-setup`

La fase estabiliza una aplicación funcional. No añade alcance por conveniencia: todo defecto se reproduce primero con un rojo y toda optimización requiere una medida.

## 6.1 — Ensayo de instalación limpia — Completada

En un entorno limpio: comprobar prerrequisitos, ejecutar `scripts/setup.ps1` dos veces, arrancar con PostgreSQL detenido, completar el recorrido, reiniciar servicios, verificar persistencia y ejecutar `scripts/test.ps1`.

Los scripts dan mensajes accionables y exit code distinto de cero ante requisitos ausentes. Nunca sobrescriben `.env` ni borran volúmenes del usuario.

Resultado verificado el 10 de agosto de 2026 desde una copia limpia de `HEAD` y un volumen PostgreSQL aislado:

- `scripts/setup.ps1` finalizó correctamente dos veces; la segunda ejecución no aplicó migraciones adicionales.
- `scripts/start.ps1` recuperó PostgreSQL detenido e inició API y Web con ambos endpoints disponibles.
- El recorrido ingrediente → receta → plan semanal persistió sus datos y la asignación después de reiniciar PostgreSQL, API y Web.
- `scripts/test.ps1` compiló en Release sin warnings y superó 212 pruebas: 59 Domain, 40 Application, 52 Integration, 46 Component y 15 End-to-End; 0 fallos y 0 omitidas.
- El entorno aislado se retiró y los servicios locales habituales se restauraron conservando su volumen original.

## 6.2 — Regresión guiada por TDD — Completada

La revisión priorizó corrección, integridad de datos, seguridad, errores de red, cancelación y lifecycle. Cada defecto confirmado se reprodujo primero con xUnit v3 sobre MTP y se corrigió con el cambio mínimo:

- **P1 — Resuelto:** una caída de la API durante alta, edición o borrado de catálogos escapaba del componente Blazor. Los cuatro catálogos conservan ahora el formulario y muestran el error recuperable.
- **P2 — Resuelto:** unidades, etiquetas y tipos de comida usaban `CancellationToken.None`. Ahora mantienen un token de lifecycle cancelado al liberar el componente.
- **P2 — Resuelto:** los conflictos de EF Core publicaban `DbUpdateException.Message`. `ProblemDetails` conserva el código `persistence.conflict`, pero devuelve un detalle estable sin información técnica.
- **P1 — Resuelto:** el recorrido E2E de persistencia conservaba un circuito Blazor del proceso Web detenido y podía competir con la reconexión automática. El navegador se desconecta de forma observable antes del reinicio y vuelve al plan después de los health checks.
- **Mejora posterior:** los listados que cargan agregados completos no se modifican sin una medición; se revisarán en 6.5.

La dependencia de `DateTime.Today` en `WeeklyPlanFormModel` se revisó expresamente. El cálculo lee el reloj una sola vez, conserva el lunes actual y avanza al siguiente desde domingo, por lo que no se demostró una regresión ni se añadió una abstracción temporal. Se elimina el ejemplo anterior porque usaba una propiedad `WeeklyPlanMealResponse.Recipe` inexistente y duplicaba el recorrido E2E que ya verifica la asignación después de reiniciar servicios.

La subfase añade nueve regresiones de componentes y una del manejador de excepciones. El gate final superó 222 pruebas: 59 Domain, 40 Application, 53 Integration, 55 Component y 15 End-to-End; 0 fallos y 0 omitidas. No cambia rutas, DTO, esquema ni migraciones. La siguiente unidad ejecutable es la subfase 6.3.

## 6.3 — Auditoría de calidad de tests — Completada

La auditoría se ejecutó el 10 de agosto de 2026 sobre las cinco suites xUnit v3/Microsoft Testing Platform. Se revisaron anti-patrones, diversidad de assertions, huecos conductuales y los puntos de integración API, componentes y E2E.

Resultado:

- **0 críticos y 0 altos:** no hay tests sin assertions, tautológicos, `async void`, esperas fijas, reloj real, excepciones tragadas, dependencia de orden ni tests omitidos. Los tests E2E usan `Expect` de Playwright y los tests de lifecycle delegan sus assertions en helpers verificables.
- **1 menor:** `Create_DescriptionIsMissing_NormalizesDescriptionToNull` comprueba únicamente `null`; es el resultado completo del contrato de normalización y no genera falsa confianza.
- **1 mejora de mantenibilidad:** 14 escenarios superan 30 líneas (4 superan 50) porque cubren recorridos completos de API, persistencia o navegador. Se conservan como escenarios de regresión coherentes y se podrán descomponer durante una futura revisión de mantenibilidad.
- Los 197 métodos de test (222 casos ejecutados por las teorías) cubren equality, boolean, null, exception, type, string, collection, negative, estado/efectos y assertions estructurales. No se detectaron huecos bloqueantes en status/body/headers/persistencia de API, DOM/efecto de componentes ni recuperación E2E tras reinicio.

El proveedor de cobertura no está referenciado todavía y no existe Cobertura XML. No se añade durante esta subfase: la configuración MTP y el diagnóstico CRAP pertenecen a 6.4, sin alterar el baseline de paquetes de 6.3.

Gate verificado: `scripts/test.ps1` superó 222/222 (59 Domain, 40 Application, 53 Integration, 55 Component y 15 End-to-End), 0 fallos y 0 omitidas; `dotnet format Friggy.sln --verify-no-changes --no-restore` limpio; auditoría NuGet sin vulnerabilidades. La siguiente unidad ejecutable es la subfase 6.4.

## 6.4 — Cobertura como diagnóstico — Completada

Se configuró `Microsoft.Testing.Extensions.CodeCoverage` 18.9.0 en las cinco suites xUnit v3/Microsoft Testing Platform. El proveedor oficial de MTP admite `--coverage-output-format cobertura` en .NET 10; se generó un fichero Cobertura independiente por proyecto para evitar sobrescrituras.

```powershell
./.codex/skills/coverage-analysis/scripts/Extract-MethodCoverage.ps1 `
  -CoberturaPath ./TestResults/coverage-analysis/raw/*/*.cobertura.xml `
  -CoverageThreshold 80 -BranchThreshold 70 -Filter below-threshold

./.codex/skills/coverage-analysis/scripts/Compute-CrapScores.ps1 `
  -CoberturaPath ./TestResults/coverage-analysis/raw/*/*.cobertura.xml `
  -CrapThreshold 30 -TopN 10
```

Resultado del diagnóstico:

- Cobertura de línea: **73.5%**; cobertura de ramas: **56.3%**; 734 métodos analizados y 9 hotspots con CRAP superior a 30.
- Los hotspots dominantes son código generado por OpenAPI y artefactos de migraciones; se excluyen de la prioridad de producto.
- El hotspot accionable es `RecipeFormModel.Validate`: complejidad 18, 36.4% de cobertura y CRAP 101.5. Con 80% de cobertura su CRAP bajaría aproximadamente a 20.6.
- Los huecos de producto se concentran en `RecipeFormModel.cs` (21 líneas), `Ingredients.razor` (16), `CatalogApiClient.cs` (14), `WeeklyPlans.razor` (14) y `RecipeForm.razor` (13).
- No se escriben tests nuevos en esta subfase: 6.4 identifica riesgo; la cobertura conductual se abordará en 6.5/6.6 con pruebas dirigidas y sin perseguir getters triviales.

El informe completo, con tabla CRAP y rutas de los cinco XML, queda en `TestResults/coverage-analysis/coverage-analysis.md`. La siguiente unidad ejecutable es la subfase 6.5.

## 6.5 — Rendimiento medido — Completada

Se midieron los listados contra PostgreSQL real usando un escenario reproducible de 12 recetas con 3 ingredientes, 3 pasos, 2 etiquetas y 3 tipos de comida, además de 8 planes con 21 asignaciones cada uno. El test `PerformanceMeasurementTests.ReadModels_RecordDurationRowsAndSql` registra duración, filas, comandos y SQL en `TestResults/performance/6.5/read-models.json`.

Resultado del diagnóstico en el mismo contenedor:

| Escenario | Duración | Filas relacionadas | Comandos SQL |
| --- | ---: | ---: | ---: |
| `recipes.list.aggregate-baseline` | 370.9 ms | 132 | 5 |
| `recipes.list.summary` | 15.9 ms | 0 | 1 |
| `weekly-plans.list.aggregate-baseline` | 36.5 ms | 168 | 1 |
| `weekly-plans.list.summary` | 6.9 ms | 0 | 1 |

La evidencia confirma impacto observable: el listado de recetas hacía cinco viajes y materializaba el grafo completo para devolver solo tres campos; el listado de planes también cargaba todas sus asignaciones. Se añadieron `ListSummariesAsync` a los repositorios existentes, con proyecciones `AsNoTracking` de raíz, y los servicios de listado usan ahora esas proyecciones. Los métodos completos se conservan para detalle, edición y borrado.

No cambian rutas, DTO públicos, esquema ni migraciones. No se añade caché ni infraestructura adicional. La siguiente unidad ejecutable es la subfase 6.6.

## 6.6 — Gate automatizado — Completada

Se creó `scripts/quality-gate.ps1` como entrada única y reproducible del gate local. Ejecuta, en orden, `dotnet restore` con `NuGet.Config`, build Release sin restore, `dotnet format --verify-no-changes`, las cinco suites mediante `scripts/test.ps1 -SkipBuild` y la auditoría de paquetes con salida JSON. El parámetro `-SkipBuild` evita recompilar cuando el gate ya ha construido la solución.

La auditoría recorre la salida estructurada de `dotnet list package --vulnerable --include-transitive --format json` y falla si encuentra vulnerabilidades. Los tests de contrato de scripts verifican el modo fail-fast, el orden de los pasos y la ausencia del separador `--` incompatible con MTP sobre .NET 10.

Gate final ejecutado el 10 de agosto de 2026:

- Build Release correcto, con 0 errores y 0 warnings.
- Formato correcto.
- 225 pruebas verdes: 59 Domain, 40 Application, 56 Integration, 55 Component y 15 End-to-End; 0 fallos y 0 omitidas.
- Auditoría NuGet sin vulnerabilidades.

La siguiente unidad ejecutable es la subfase 6.7 — Piloto y cierre.

## 6.7 — Piloto y cierre — Completada

Se ejecutó un piloto técnico local y sus resultados quedaron validados antes de retirar los documentos operativos históricos. El ensayo cubrió dos ejecuciones idempotentes de `setup.ps1`, `start.ps1`, navegación de las pantallas principales, el recorrido E2E completo y persistencia después de reiniciar servicios.

Resultados:

- `setup.ps1`: 17.19 s en la primera ejecución y 10.24 s en la segunda; la segunda no aplicó migraciones.
- `start.ps1`: API y Web disponibles en 14.00 s mediante health checks.
- E2E: 15/15 pruebas correctas en 29.1 s.
- No se confirmaron defectos bloqueantes, pérdida de datos ni errores funcionales de severidad alta.
- La valoración con una persona real queda explícitamente pendiente: esta sesión fue técnica y automatizada.

README, ADR y definición del MVP reflejan ahora el flujo Web → HTTP → API, PostgreSQL/Docker y los scripts de operación. La fase 6 queda cerrada como **candidata a piloto guiado local**; el trabajo posterior al MVP se abre en una fase independiente.
