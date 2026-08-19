using System.Globalization;

namespace Friggy.Domain.Catalogs;

/// <summary>
/// Value object <see cref="CatalogName"/> que representa el nombre o la descripción
/// para una entidad de tipo catálogo (tabla maestra). Contiene la lógica de negocio
/// para este tipo de propiedades
/// </summary>
public sealed record CatalogName
{
    /// <summary>
    /// Tamaño máximo permitido para <seealso cref="Value"/> y 
    /// <seealso cref="Normalized"/>.
    /// </summary>
    public const int MaximumLength = 120;

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private CatalogName()
    {
        Value = string.Empty;
        Normalized = string.Empty;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="value">
    /// Nombre o descripción de una propiedad,
    /// </param>
    private CatalogName(string value)
    {
        Value = value;
        Normalized = value.ToUpper(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Nombre o descripción de una propiedad,
    /// </summary>
    public string Value { get; private init; }

    /// <summary>
    /// Valor normalizado (en mayúsculas) de <seealso cref="Value"/>.
    /// </summary>
    public string Normalized { get; private init; }

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="value">
    /// Nombre o descripción de la propiedad.
    /// </param>
    /// <param name="requiredCode">
    /// Código de error en caso de que ocurra algún fallo al crear la propiead.
    /// </param>
    /// <returns>
    /// Objeto <see cref="CatalogName"/>.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el valor es una cadena vacía o 
    /// cuando supera la longitud máxima.
    /// </exception>
    public static CatalogName Create(string? value, string requiredCode)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainValidationException(requiredCode, "El nombre es obligatorio.");
        }

        if (trimmed.Length > MaximumLength)
        {
            throw new DomainValidationException(
                requiredCode.Replace("required", "too-long", StringComparison.Ordinal),
                $"El nombre no puede superar {MaximumLength} caracteres.");
        }

        return new CatalogName(trimmed);
    }
}
