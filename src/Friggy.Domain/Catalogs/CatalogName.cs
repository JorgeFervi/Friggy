using System.Globalization;

namespace Friggy.Domain.Catalogs;

public sealed record CatalogName
{
    public const int MaximumLength = 120;

    private CatalogName()
    {
        Value = string.Empty;
        Normalized = string.Empty;
    }

    private CatalogName(string value)
    {
        Value = value;
        Normalized = value.ToUpper(CultureInfo.InvariantCulture);
    }

    public string Value { get; private init; }

    public string Normalized { get; private init; }

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
