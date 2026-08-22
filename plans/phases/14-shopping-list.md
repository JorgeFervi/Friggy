# Fase 14 — Lista de la compra calculada

- **Estado:** Planificada
- **Estimación:** 3–5 días
- **Dependencias:** [Fase 12](../completed/phases/12-daily-planning.md); puede ejecutarse independientemente de la Fase 13
- **Guía ejecutable:** [Implementación de la fase 14](../implementation/14-shopping-list-implementation.md)

## Resultado esperado

Ofrecer una vista calculada para un intervalo inclusivo de fechas que compare las necesidades de las comidas planificadas con el inventario actual y muestre cuánto comprar por combinación exacta de ingrediente y unidad.

## Fórmula vinculante

```text
necesidad = suma(cantidad de línea de receta × comensales de la comida)
disponible = suma(lotes con cantidad positiva y no caducados hoy)
a comprar = máximo(0, necesidad − disponible)
```

La agregación usa exclusivamente la clave `(IngredientId, UnitTypeId)`. No hay conversión entre unidades aunque pertenezcan al mismo ingrediente.

## Decisiones vinculantes

- La lista es una consulta pura y no crea entidad, tabla, historial ni operaciones de escritura.
- `from` y `to` son obligatorios, inclusivos y admiten el mismo día; solo se rechaza `from > to`.
- No se impone un límite funcional al intervalo. La consulta filtra y agrupa en PostgreSQL antes de materializar para evitar cargar todos los planes, recetas o lotes.
- Solo cuentan entradas en estado `Planned`. Las omitidas y completadas se excluyen; los huecos sin receta no generan necesidad.
- “Inventario actual” significa lotes utilizables el día del cálculo. La primera versión no pronostica que un lote pueda caducar dentro de un intervalo futuro.
- La respuesta conserva requerido, disponible y cantidad a comprar, incluidas líneas completamente cubiertas para que la comparación sea visible.

## Alcance

1. Contrato de consulta y DTO específicos bajo `Application/ShoppingLists`.
2. Read model EF Core con agregación en base de datos y reloj controlable.
3. `GET /api/shopping-list?from=yyyy-MM-dd&to=yyyy-MM-dd` con `ProblemDetails` estable.
4. Cliente HTTP y página `/shopping-list` con selector de fechas, estados vacío/error/loading y tabla accesible.
5. Pruebas de cálculo, filtros de estado, caducidad, unidades y recorridos completos.

## Fuera de alcance

- Persistir, editar, marcar, compartir, imprimir o exportar la lista.
- Agrupar por categorías o secciones del supermercado.
- Conversiones de unidades, reservas de inventario o asignación de lotes a días futuros.
- Consumir inventario al calcular la lista.

## Orden de ejecución

1. Escribir rojos de Application para intervalo, multiplicación por comensales, agregación y estados excluidos.
2. Definir un puerto de lectura específico y un fake manual; no ampliar el agregado con lógica de presentación.
3. Implementar la proyección y agregación EF Core, revisar SQL y probarla con PostgreSQL real.
4. Publicar la ruta GET y sus errores de formato/rango.
5. Añadir navegación, cliente y página Blazor sin estado global ni persistencia local.
6. Ejecutar pruebas de rendimiento focalizadas, E2E, capturas visuales revisadas y gate completo.

## Criterios de aceptación

- Un intervalo de un día, varios días o días sin plan produce un resultado determinista.
- Las líneas repetidas entre recetas y días se agregan por ingrediente y unidad exactos.
- Los comensales multiplican cada cantidad de receta.
- El inventario se descuenta una sola vez del total agregado y nunca genera cantidades negativas.
- Lotes agotados o caducados antes de hoy no cuentan; los que caducan hoy sí cuentan.
- Comidas omitidas, completadas o sin receta no aparecen en la necesidad.
- Cambiar un plan o un lote se refleja en el siguiente cálculo sin regenerar ni sincronizar una entidad.
- La consulta respeta cancelación y no presenta N+1 ni materialización completa de tablas.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Descontar el mismo stock por cada día | agregar primero toda la necesidad y todo el inventario por la misma clave |
| Contar comidas ya resueltas | filtro explícito `Status == Planned` cubierto en Application e Integration |
| Intervalos grandes | filtros e `GROUP BY` traducidos a SQL, índices por fecha/estado y prueba de consulta |
| Confundir unidades | líneas separadas por `UnitTypeId`, nombre y símbolo visibles |
| Interpretar caducidad futura como previsión | indicar fecha de cálculo y documentar la limitación en la interfaz |

## Handoff

La fase concluye con una vista de compra reproducible y sin efectos laterales. Cualquier futura edición, persistencia o conversión deberá abrir una fase distinta.
