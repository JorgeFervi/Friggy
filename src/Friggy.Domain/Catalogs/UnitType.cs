namespace Friggy.Domain.Catalogs;

public sealed class UnitType
{
    public const int MaximumSymbolLength = 20;

    private UnitType()
    {
        Name = null!;
        Symbol = string.Empty;
    }

    private UnitType(Guid id, CatalogName name, string symbol)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public string Symbol { get; private set; }

    public static UnitType Create(string? name, string? symbol) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "unit-type.name.required"),
            ValidateSymbol(symbol));

    public void Update(string? name, string? symbol)
    {
        Name = CatalogName.Create(name, "unit-type.name.required");
        Symbol = ValidateSymbol(symbol);
    }

    private static string ValidateSymbol(string? symbol)
    {
        var trimmed = symbol?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainValidationException(
                "unit-type.symbol.required",
                "El símbolo es obligatorio.");
        }

        if (trimmed.Length > MaximumSymbolLength)
        {
            throw new DomainValidationException(
                "unit-type.symbol.too-long",
                $"El símbolo no puede superar {MaximumSymbolLength} caracteres.");
        }

        return trimmed;
    }
}
