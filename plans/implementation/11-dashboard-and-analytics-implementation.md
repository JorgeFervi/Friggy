# Implementación ejecutable — Fase 11

- **Fase relacionada:** [Fase 11 — Panel principal y analítica](../phases/11-dashboard-and-analytics.md)
- **Skills aplicables:** `architecture`, `xunit`, `entity-framework-core`, `minimal-apis`, `blazor`, `quality-ci`

El panel es un consumidor de datos confirmados. No debe cargar agregados completos ni recalcular en Razor reglas que pertenecen a Application.

## 11.1 — Definir métricas verificables

1. Fijar el plan seleccionado como periodo del primer incremento.
2. Definir próxima comida, estados de cumplimiento, agrupación por etiqueta y desempates de rankings.
3. Definir ingrediente utilizado como consumo vinculado a una comida completada; excluir alta, ajuste, descarte y consumo manual.
4. Escribir tests de Application con conjuntos pequeños que permitan reconciliar cada cifra.

## 11.2 — Puertos y proyecciones

1. Crear un único contrato de respuesta del panel compuesto por secciones independientes.
2. Definir puertos de lectura orientados a la información requerida, sin repositorio genérico.
3. Implementar proyecciones `AsNoTracking` y agregaciones traducibles por PostgreSQL.
4. Probar plan inexistente, plan vacío, empates y cancelación.

No persistir totales derivados.

## 11.3 — API del panel

1. Exponer una ruta de consulta para un plan seleccionado.
2. Devolver `404` para plan inexistente y una respuesta vacía válida para plan sin datos.
3. Documentar orden, periodo y significado de cada métrica.
4. Probar status, cuerpo y ausencia de entidades persistentes en el contrato.

## 11.4 — Panel principal

1. Sustituir la portada estática por selector de plan y secciones independientes.
2. Mostrar próxima comida, carencias, calendario y resumen de cumplimiento.
3. Permitir abrir desde cada sección el detalle fuente correspondiente.
4. Tratar loading, error parcial y estado vacío sin ocultar toda la página.

## 11.5 — KPIs y gráficos accesibles

1. Mostrar totales por etiqueta, recetas e ingredientes.
2. Empezar con HTML/CSS y tablas o barras semánticas; no añadir librería de gráficos sin necesidad demostrada.
3. Proporcionar valores textuales, títulos y orden determinista.
4. Probar el contenido y las relaciones, no píxeles, salvo que se adopte después regresión visual.

## 11.6 — Rendimiento y gate

1. Medir consultas sobre un volumen reproducible de planes, comidas y movimientos.
2. Registrar comandos y duración; corregir N+1 o materialización excesiva antes de considerar caché.
3. E2E: seleccionar plan y navegar desde carencia, próxima comida y ranking a sus detalles.
4. Ejecutar `scripts/quality-gate.ps1` y documentar límites conocidos.
