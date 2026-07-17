# ADR 001: Uso de C# Minimal APIs para el desarrollo del Backend

* **Estatus:** Aceptado
* **Fecha:** 2026-07-17
* **Autor:** Jorge Fervi

## Contexto

La aplicación Friggy necesita un backend donde se encuentre la lógica de negocio y los casos de uso. Tengo un conocimiento sólido y años de experiencia desarrollando en el ecosistema de .NET con el lenguaje de programación C# y necesito desarrollar la aplicación en el menor tiempo posible.

## Decisión

Elijo usar **ASP.NET Core**  como el framework para el desarrollo del backend. El enfoque de diseño será **Minimal APIs**.

La decisión se fundamenta en las siguientes restricciones y realidades:

* **Curva de aprendizaje cero:** Ya domino el lenguaje C# y las últimas versiones de .NET. No necesito aprender nada nuevo. Me ahorro tiempo de aprendizaje.

* **Reducción de Boilerplate:** Las Minimal APIs eliminan la necesidad de configurar controladores extensos, permitiendo mapear rutas directamente y acelerar el desarrollo.

* **Rendimiento:** Ofrece un rendimiento ligeramente superior y menor consumo de memoria que el enfoque tradicional de controladores.

## Consecuencias

* **Positivo:** Alta velocidad de desarrollo inicial y alineación total con mis capacidades actuales. Facilidad para empaquetar en contenedores Docker ligeros.

* **Negativo/Riesgo:** A medida que la aplicación crezca, el archivo `Program.cs` puede volverse inmanejable si no aplico patrones de diseño adecuados.