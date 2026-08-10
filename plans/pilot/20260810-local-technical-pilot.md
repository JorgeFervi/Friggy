# Registro de piloto técnico local — 2026-08-10

## Alcance

Sesión técnica reproducible del MVP monousuario local. Se validaron instalación, arranque, navegación principal, recorrido E2E y persistencia tras reinicio. No hubo una persona externa realizando la sesión, por lo que las observaciones de usabilidad humana quedan pendientes del piloto guiado.

## Entorno

| Componente | Valor observado |
| --- | --- |
| .NET SDK | 10.0.302 |
| Docker Engine | 29.5.3 |
| PostgreSQL | 17.6-alpine |
| Web | `http://localhost:5179` |
| API | `http://localhost:5292` |

## Ejecución

1. `scripts/setup.ps1`: correcto en **17.19 s**; aplicó la migración pendiente y dejó PostgreSQL saludable.
2. `scripts/setup.ps1` por segunda vez: correcto en **10.24 s**; informó que no había migraciones pendientes.
3. `scripts/start.ps1`: correcto en **14.00 s**; API y Web respondieron a sus comprobaciones de salud.
4. Inspección de solo lectura en `/`, `/ingredients`, `/recipes` y `/weekly-plans`: navegación, formularios, tablas y estados vacíos visibles.
5. Recorrido `Friggy.EndToEndTests`: **15/15** correctos en **29.1 s**, incluyendo creación de receta, asignación semanal y recuperación después de reiniciar los servicios.

## Observaciones

| Observación | Clasificación | Decisión |
| --- | --- | --- |
| Instalación repetible y mensajes accionables | Resultado positivo | Mantener scripts actuales |
| Flujo Web → HTTP → API → PostgreSQL verificado por E2E | Resultado positivo | Candidato a piloto guiado |
| No se detectaron errores funcionales, pérdida de datos ni bloqueos de severidad alta | Sin defecto confirmado | No abrir regresión |
| No hubo participante humano para valorar lenguaje, esfuerzo o accesibilidad | Limitación de la sesión técnica | Registrar como tarea del piloto guiado, fuera de esta corrección |

No se modificaron datos mediante SQL directo ni se añadieron credenciales. Los datos creados por recorridos anteriores permanecen en el volumen local de desarrollo.

## Salida

El MVP queda como **candidato a piloto guiado local**. La fase posterior al MVP se mantiene separada: frigorífico virtual e inventario, después lista de la compra, y más adelante integración con IA cuando los datos domésticos sean fiables.
