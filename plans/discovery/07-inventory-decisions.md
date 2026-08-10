# Decisiones previas — Frigorífico virtual e inventario

- **Estado:** Pendiente de validación del propietario del producto
- **Efecto:** La Fase 7 no debe abrirse hasta confirmar o sustituir todas las propuestas

Estas decisiones separan el inventario de la futura lista de la compra y evitan que el modelo de datos se defina accidentalmente desde la interfaz o desde EF Core.

| Decisión | Propuesta inicial | Motivo | Resolución |
|---|---|---|---|
| Representación del stock | Lotes por ingrediente, cantidad, unidad y caducidad opcional | Dos compras del mismo ingrediente pueden caducar en fechas diferentes | Pendiente |
| Ubicaciones | No incluirlas en el primer incremento salvo evidencia del piloto | Frigorífico, congelador y despensa amplían filtros y reglas sin ser necesarios para calcular disponibilidad | Pendiente |
| Operaciones | Añadir, consumir, ajustar y descartar cantidades explícitamente | Cubre el mantenimiento manual sin automatización oculta | Pendiente |
| Stock negativo | No permitirlo | Evita que un error de consumo se convierta en inventario ficticio | Pendiente |
| Conversiones | Solo unidades dimensionalmente compatibles y con factor explícito | Masa, volumen y unidades no se convierten entre sí sin información adicional | Pendiente |
| Unidad incompatible | Mostrar la cantidad como no cubierta; no inferir densidades ni equivalencias | Una conversión silenciosa produciría cálculos incorrectos | Pendiente |
| Integración con el plan semanal | Calcular disponibilidad y carencias, sin descontar stock automáticamente | Planificar una comida no demuestra que se haya cocinado | Pendiente |
| Lista de la compra | Fase posterior, generada desde carencias y confirmada por el usuario | Mantiene la Fase 7 centrada en datos domésticos fiables | Pendiente |
| Usuarios y ejecución | Mantener monousuario y local | Autenticación y despliegue público siguen fuera del alcance | Pendiente |
| Problema mínimo | Conocer cuánto hay de cada ingrediente y qué caduca, y comparar esas existencias con una planificación | Produce valor antes de automatizar compras o consumo | Pendiente |

## Preguntas de validación

- ¿Necesitas conocer el historial de movimientos o solo el estado actual de cada lote?
- ¿Una caducidad ausente significa que el alimento no caduca o que no se conoce la fecha?
- ¿El consumo manual debe elegir un lote o aplicar primero el que caduca antes?
- ¿Las cantidades de una receta se comparan por semana completa o por una comida seleccionada?
- ¿Qué debe ver el usuario cuando hay existencias parciales?

## Gate para abrir la Fase 7

- Piloto humano registrado con cero defectos bloqueantes abiertos.
- Todas las resoluciones anteriores confirmadas o reemplazadas.
- Límites de la primera entrega escritos sin lista de la compra, IA, autenticación ni despliegue público.
- Contratos, migración y escenarios PostgreSQL identificados en un plan de fase separado.
- `scripts/quality-gate.ps1` en verde sobre el commit de partida.

La implementación debe conservar los proyectos actuales. Inventario será una capacidad nueva dentro de las capas existentes, no un microservicio, un proyecto adicional ni una excusa para introducir CQRS.
