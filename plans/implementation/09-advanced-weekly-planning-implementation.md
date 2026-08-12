# Implementación ejecutable — Fase 9

- **Fase relacionada:** [Fase 9 — Planificación semanal avanzada](../phases/09-advanced-weekly-planning.md)
- **Skills aplicables:** `architecture`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`

Esta fase amplía el agregado semanal. No cambia movimientos ya registrados ni reabre comidas completadas.

> **Progreso:** subfase 9.3 implementada por indicación expresa pese al gate pendiente de 9.2. Compilación, formato y suites sin Docker están verdes; la validación PostgreSQL de 9.2 y 9.3 queda pendiente porque el entorno actual no permite acceder al daemon. No iniciar 9.4 hasta ejecutar esas pruebas.

## 9.1 — Tipos de comida por día — Completada

1. Escribir rojos para añadir, ordenar y retirar huecos de comida dentro de un día del plan.
2. Decidir una identidad estable para el hueco, independiente de su posición visual.
3. Impedir duplicados del mismo tipo en el mismo día.
4. No permitir retirar un hueco completado; una asignación futura debe retirarse explícitamente antes.

`MealPlanSlot` conserva una identidad independiente de su orden y de la asignación. Durante 9.1 los huecos se mantuvieron solo en Domain y se ignoraron explícitamente en EF Core hasta completar su persistencia en 9.2.

## 9.2 — Persistencia y compatibilidad — Implementada, validación PostgreSQL pendiente

1. Migrar el calendario actual creando huecos equivalentes a sus tipos globales existentes.
2. Conservar asignaciones, raciones y estados de la Fase 8.
3. Añadir índices y restricciones con PostgreSQL real.
4. Probar lectura del esquema anterior y round-trip completo.

La migración crea los huecos equivalentes a todos los tipos globales en los siete días de cada plan existente antes de activar la FK de las asignaciones. Los planes nuevos conservan el mismo calendario inicial; PostgreSQL protege tipo y orden únicos por día.

## 9.3 — Horario y preparación — Implementada, validación PostgreSQL pendiente

1. Añadir hora local opcional a cada hueco planificado.
2. Validar el formato en Domain/Application y transportar sin conversiones de zona horaria en el entorno local monousuario.
3. Derivar inicio de preparación restando el tiempo estimado total de receta.
4. Probar cruces de medianoche y ausencia de hora o receta.

No persistir el inicio derivado.

La hora prevista se persiste como `time without time zone`, se recibe y normaliza como `HH:mm` y puede eliminarse. El inicio de preparación es un `DateTime` local sin `Kind` de zona, calculado con la fecha del hueco y `Recipe.EstimatedTime`; permanece nulo sin hora o receta y no se persiste.

## 9.4 — Comida omitida o sustituida

1. Añadir transición desde planificada a omitida con motivo y alternativa descriptiva.
2. Una comida omitida no consume lotes ni cuenta como completada.
3. No modificar estados completados existentes.
4. Si se prepara otra receta registrada, exigir sustituir antes la asignación y usar el flujo normal de completado.

## 9.5 — API y calendario Blazor

1. Ampliar contratos para administrar huecos, hora y estado omitido.
2. Mostrar solo tipos elegidos para cada día y permitir su edición accesible.
3. Presentar hora prevista e inicio de preparación junto a la receta.
4. Probar errores recuperables y conservación del formulario con bUnit.

## 9.6 — Gate

1. Integration: migración, restricciones y transiciones.
2. E2E: configurar días diferentes, asignar horas, omitir una comida y completar otra.
3. Verificar que inventario solo cambia por la completada.
4. Ejecutar `scripts/quality-gate.ps1` y cerrar la fase.
