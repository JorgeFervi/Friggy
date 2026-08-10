# Fase 6 — Estabilización y piloto

- **Estado:** En progreso — subfase 6.3 completada el 10 de agosto de 2026
- **Estimación:** 1–3 días
- **Dependencias:** [Fase 5](05-full-integration.md)
- **Guía ejecutable:** [Implementación de la fase 6](../implementation/06-stabilization-and-pilot-implementation.md)

## Resultado esperado

Un MVP instalable desde cero, sin defectos conocidos de severidad alta, probado con usuarios piloto locales y acompañado de documentación operativa.

## Precondiciones

- Recorrido Playwright principal en verde.
- No quedan comportamientos funcionales pendientes.

## Orden de ejecución

1. Ejecutar revisión `dotnet-review` priorizando corrección, datos, seguridad y regresión.
2. Auditar paquetes, analyzers, formato, warnings y límites de arquitectura.
3. Ejecutar todas las suites desde un checkout y base vacíos.
4. Corregir cada defecto mediante test rojo previo.
5. Medir consultas relevantes y aplicar optimización solo ante evidencia.
6. Preparar datos de ejemplo y checklist de sesión piloto.
7. Realizar el piloto, registrar observaciones y clasificar cambios como defecto o futura mejora.
8. Actualizar README, ADR, planes y limitaciones conocidas.

## Entregables

- Quality gate reproducible y suite completa verde.
- Guía de instalación, inicio, pruebas y resolución de problemas.
- Datos de ejemplo opcionales.
- Registro de piloto, defectos resueltos y backlog posterior al MVP.

## Criterios de salida

- Una persona nueva puede ejecutar `setup.ps1`, `start.ps1` y completar el recorrido sin conocimiento interno.
- No existen vulnerabilidades altas, errores de compilación, tests omitidos o warnings aceptados sin documentar.
- Todos los defectos corregidos tienen prueba de regresión.
- Reinicios y migración desde base vacía están verificados.
- Las observaciones no imprescindibles quedan fuera del MVP y registradas para una fase posterior.

## Agentes y skills

- `dotnet-review`: revisión de corrección, arquitectura y cobertura.
- `dotnet-build`: reproducibilidad y quality gate.
- `dotnet-data`: revisión de migraciones y consultas medidas.
- Skills: `code-review`, `quality-ci`, `coverage-analysis`, `test-anti-patterns`, `optimizing-ef-core-queries` cuando proceda.

## Cierre

Marcar el MVP como candidato a piloto, congelar su esquema y abrir un plan separado para frigorífico virtual y lista de compra.
