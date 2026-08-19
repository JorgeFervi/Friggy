namespace Friggy.Domain.Catalogs;

/// <summary>
/// Clase <see cref="DomainValidationException"/> que representa un error lanzado
/// desde la capa de dominio cuando se incumple una regla de negocio de esta capa.
/// </summary>
/// <param name="code">
/// Código de error.
/// </param>
/// <param name="message">
/// Mensaje descriptivo asociado al error.
/// </param>
public sealed class DomainValidationException(string code, string message)
    : Exception(message)
{
    public string Code { get; } = code;
}
