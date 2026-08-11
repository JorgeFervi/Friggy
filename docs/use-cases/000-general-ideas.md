# Documento general - Ideas de casos de uso

Este documento sirve como principal plan de definición para los casos de uso que podrá podrá abordar la aplicación, recopilando las primeras ideas y posibles escenarios de uso. Su contenido se irá ampliando y refinando a medida que avance el desarrollo.


Ideas principales para la Fase 1 del proyecto de desarrollo de la aplicación:

- Registro de recetas de comidas. 
  - Datos que incluirán:
    - Nombre de la receta
    - Ingredientes que se van a usar
    - Cantidades de alimentos y tipo de unidad de medida
    - Tiempo estimado
    - Etiquetas para clasificar las recetas (p. ej.: vegano, sin gluten)
    - Imagen del resultado final de la receta
    - Indicar la preferencia de tipo de comida (desayuno, comida, cena, primer plato, segundo plato, postre, etc)
  - Utilidad IA (Opcional):
    - Que te sugiera nuevas recetas basándose en tus preferencias o recetas actuales.

- Registro de los pasos a seguir en las recetas. 
  - Datos que incluirán:
    - Ingredientes utilizados
    - Descripción del paso
    - Tiempo estimado
    - Imagen 
  - Utilidad IA (Opcional):
    - Mejora de estilo de escritura.
    - Que sugiera recomendaciones alternativas a los pasos actuales.

- Registro de alimentos. 
  - Datos que incluirán:
    - Nombre
    - Imagen
    - Características
    - Vitaminas/Propiedades
    - Breve descripción (opcional)
  - Utilidad IA (Opcional):
    - Que registre automáticamente un ingrediente o alimento usando el tono o estilo de escritura de otros alimentos.
  
- Registro de plan semanal de comidas.
  - Datos que incluirá:
    - Nombre del plan
    - Definición de tipos de comida
    - Fecha de inicio y fecha fin (este último opcional)
    - Breve descripción
    - Indicar que se va a comer a cada hora del día
  - Utilidad IA (Opcional):
    - Que establezca automáticamente las comidas para cada uno de los días.
  
Ideas posteriores al MVP:

- Registrar existencias domésticas mediante lotes con cantidad, unidad y caducidad, conservando el historial de sus movimientos.
- Comparar el inventario utilizable con un plan semanal seleccionado, teniendo en cuenta las raciones asignadas a cada receta.
- Registrar el consumo únicamente cuando una comida se complete y mediante selección manual de los lotes utilizados.
- Crear la lista de la compra en una fase independiente posterior, a partir de las carencias calculadas y confirmadas por el usuario.

## Definición del MVP

La selección de funcionalidades para la primera versión, su modelo de datos y el calendario previsto se detallan en [Definición del MVP](001-mvp.md).
