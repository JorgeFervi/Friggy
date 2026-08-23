# Fase 16 — Conversiones y usos contextuales de unidades

- **Estado:** Planificada
- **Estimación:** 6–9 días
- **Dependencias:** catálogo de unidades, recetas, inventario, finalización de comidas y lista de compra actuales
- **Guía ejecutable:** [Implementación de la fase 16](../implementation/16-unit-conversions-implementation.md)

## Resultado esperado

Permitir que Friggy compare y consuma cantidades expresadas en unidades compatibles y distinguir qué unidades pueden elegirse al cocinar y cuáles pueden usarse al registrar compras e inventario. Una receta podrá pedir cucharadas de aceite, mientras inventario y lista de compra trabajan con mililitros o litros.

## Diagnóstico vinculante

Actualmente recetas, inventario, necesidades y lista de compra usan la coincidencia exacta `(IngredientId, UnitTypeId)`. Esto impide que `1 l` disponible cubra una necesidad de `15 ml` o `1 cda` y permite que una lista calculada proponga comprar cucharadas.

La conversión debe compartir semántica con:

- cálculo diario de requerido, disponible y faltante;
- lista de compra multifecha;
- selección y validación de lotes al completar una comida;
- consumo y cálculo del remanente después de completar;
- filtros de unidades de los formularios de receta e inventario.

## Modelo funcional cerrado

Cada unidad incorpora:

| Dato | Regla |
|---|---|
| Dimensión | `Unconverted`, `Mass`, `Volume` o `Count` |
| Factor base | decimal positivo respecto a g, ml o ud; para `Unconverted` siempre es 1 |
| Uso en cocina | permite seleccionar la unidad en nuevas líneas de receta |
| Uso en compra | permite registrar inventario y ser unidad de salida en comparaciones |

Conversiones iniciales:

| Unidad | Dimensión | Factor base | Cocina | Compra |
|---|---|---:|---:|---:|
| g | Mass | 1 | Sí | Sí |
| kg | Mass | 1000 | Sí | Sí |
| ml | Volume | 1 | Sí | Sí |
| l | Volume | 1000 | Sí | Sí |
| ud | Count | 1 | Sí | Sí |
| cdta | Volume | 5 | Sí | No |
| cda | Volume | 15 | Sí | No |

```text
cantidad base = cantidad × factor de la unidad origen
cantidad destino = cantidad base ÷ factor de la unidad destino
```

Dos cantidades son compatibles cuando pertenecen a la misma dimensión convertible. Las unidades `Unconverted` solo son compatibles consigo mismas.

## Agregación y unidad de salida

- Masa, volumen y conteo se agregan por `(IngredientId, Dimension)` después de normalizar a base.
- Las unidades `Unconverted` conservan la clave exacta `(IngredientId, UnitTypeId)`.
- Lista de compra y necesidades siempre devuelven una unidad habilitada para compra.
- Para elegirla se toma la cantidad requerida normalizada y se selecciona la unidad de compra con el mayor factor que produzca un valor mayor o igual que 1. Si ninguna cumple, se usa la de menor factor.
- Los empates se resuelven por nombre normalizado y después por identificador.
- Requerido, disponible y faltante se expresan en la misma unidad elegida.
- Los cálculos usan `decimal` sin redondeos intermedios. La Web conserva su formato de hasta tres decimales.

## Decisiones vinculantes

- No se crea una tabla de conversiones por pares: dimensión y factor evitan duplicados, ciclos y recorridos transitivos.
- No se convierte entre masa, volumen y conteo.
- No se admiten densidades por ingrediente, equivalencias de envases ni presentaciones comerciales.
- Inventario se considera contexto de compra: los lotes nuevos solo admiten unidades habilitadas para compra.
- Las cantidades y movimientos de cada lote siguen almacenados en la unidad original del lote.
- Al completar una comida, la cantidad indicada permanece en la unidad del lote; solo validación y remanente usan cantidades normalizadas.
- Cambiar dimensión o factor queda prohibido cuando la unidad ya está referenciada. Nombre, símbolo y usos sí pueden cambiar.
- Una unidad deshabilitada puede seguir apareciendo en recetas o lotes existentes, pero no elegirse en nuevas referencias.
- Al editar una receta se permite conservar una unidad histórica deshabilitada, pero no asignarla a una línea nueva ni cambiar otra línea hacia ella.
- Puede deshabilitarse una unidad para ambos contextos como mecanismo de retirada sin borrar datos.

## Compatibilidad y migración

- Las siete unidades iniciales reciben dimensión, factor y usos mediante migración explícita.
- Las unidades personalizadas existentes migran como `Unconverted`, factor 1 y habilitadas para cocina y compra, conservando el comportamiento exacto anterior.
- Las peticiones antiguas de alta que solo envían nombre y símbolo usan esos mismos defaults.
- En actualización, omitir campos nuevos conserva su configuración actual.
- No se reescriben cantidades de recetas, lotes ni movimientos.

## Alcance

1. Invariantes de unidad, dimensión y factor en Domain.
2. Contratos de catálogo y validaciones contextuales en Application.
3. Migración PostgreSQL y consultas normalizadas sin N+1.
4. Conversión coherente en necesidades, lista de compra y finalización.
5. Formularios de unidad, receta, inventario y finalización adaptados al contexto.
6. Pruebas Domain, Application, Integration, API, bUnit, E2E y rendimiento.

## Fuera de alcance

- Conversión masa–volumen mediante densidad del ingrediente.
- Unidades dependientes del ingrediente como lata, botella, paquete o pieza de peso variable.
- Redondeo a tamaños de envase o propuesta de número de paquetes.
- Conversión no lineal, rangos, cantidades aproximadas o fracciones visuales.
- Cambio de unidad de un lote después de crearlo.
- Persistencia o edición de la lista de compra.

## Orden de ejecución

1. Proteger con tests Domain las dimensiones, factores, compatibilidad y conversión.
2. Ampliar catálogo y contratos manteniendo compatibilidad de peticiones antiguas.
3. Crear y verificar la migración con bases vacías y upgrades con datos personalizados.
4. Aplicar restricciones contextuales a recetas e inventario.
5. Normalizar necesidades y lista de compra.
6. Adaptar finalización, selección de lotes y remanentes convertidos.
7. Actualizar UI, recorridos E2E, rendimiento, documentación y gate completo.

## Criterios de aceptación

- `1 kg` y `1000 g` del mismo ingrediente se comparan como cantidades equivalentes.
- `1 cda` se normaliza a `15 ml` y nunca aparece como unidad de compra.
- Un lote en litros puede cubrir y ser consumido por una receta expresada en mililitros o cucharadas.
- El mismo ingrediente en dimensiones distintas conserva líneas separadas.
- Una unidad no habilitada para cocina no puede añadirse a una receta nueva.
- Una unidad no habilitada para compra no puede usarse al crear un lote.
- Recetas y lotes históricos siguen operables después de deshabilitar su unidad.
- Masa y volumen nunca se compensan entre sí.
- La lista de compra sigue calculada y de solo lectura.
- No hay redondeo intermedio, N+1, escrituras al consultar ni consumo duplicado al reintentar.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Reinterpretar cantidades históricas | bloquear cambio de dimensión/factor de unidades referenciadas |
| Convertir magnitudes incompatibles | dimensión obligatoria y compatibilidad validada en Domain |
| Seleccionar una unidad arbitraria | algoritmo de salida único y determinista |
| Ocultar una unidad histórica | incluir la selección actual como opción heredada no reutilizable |
| Conversión parcial o divergente | política compartida para necesidades, compra y finalización |
| Regresión de rendimiento | agrupación en PostgreSQL, catálogo cargado una vez y medición de consultas |
| Migración de unidades personalizadas | fallback `Unconverted` exacto y prueba real de upgrade |

## Handoff

La fase termina cuando todas las comparaciones y consumos usan la misma política, las unidades se filtran por contexto y el gate completo está verde. Las equivalencias específicas de ingrediente o presentación comercial deben abrir una fase independiente.
