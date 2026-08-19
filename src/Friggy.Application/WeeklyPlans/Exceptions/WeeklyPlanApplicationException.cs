namespace Friggy.Application.WeeklyPlans.Exceptions;

/// <summary>
/// Clase base para los errores producidos al ejecutar casos de uso de planes
/// semanales.
/// </summary>
public abstract class WeeklyPlanApplicationException(string code, string message)
    : Exception(message)
{
    /// <summary>
    /// Código que identifica el fallo de aplicación.
    /// </summary>
    public string Code { get; } = code;
}
