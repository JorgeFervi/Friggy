namespace Friggy.Domain.Catalogs;

/// <summary>
/// Clase <see cref="UnitType"/> que representa una unidad de medida usada
/// para expresar cantidades de ingredientes.
/// </summary>
public sealed class UnitType
{
    /// <summary>
    /// Tamaño máximo permitido para el símbolo de la unidad.
    /// </summary>
    public const int MaximumSymbolLength = 20;

    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private UnitType()
    {
        Name = null!;
        Symbol = string.Empty;
    }

    /// <summary>
    /// Constructor usado por los métodos <see cref="Create"/> y
    /// <see cref="Update"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar a la unidad de forma interna.
    /// </param>
    /// <param name="name">
    /// Nombre de la unidad.
    /// </param>
    /// <param name="symbol">
    /// Símbolo de la unidad.
    /// </param>
    private UnitType(Guid id, CatalogName name, string symbol)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
    }

    /// <summary>
    /// Código único para identificar a la unidad de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre de la unidad.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Símbolo de la unidad.
    /// </summary>
    public string Symbol { get; private set; }

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre de la unidad.
    /// </param>
    /// <param name="symbol">
    /// Símbolo de la unidad.
    /// </param>
    /// <returns>
    /// Objeto <see cref="UnitType"/>.
    /// </returns>
    public static UnitType Create(string? name, string? symbol) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "unit-type.name.required"),
            ValidateSymbol(symbol));

    /// <summary>
    /// Método para cambiar el nombre y el símbolo de la unidad.
    /// </summary>
    /// <param name="name">
    /// Nuevo nombre que reemplazará al anterior.
    /// </param>
    /// <param name="symbol">
    /// Nuevo símbolo que reemplazará al anterior.
    /// </param>
    public void Update(string? name, string? symbol)
    {
        Name = CatalogName.Create(name, "unit-type.name.required");
        Symbol = ValidateSymbol(symbol);
    }

    /// <summary>
    /// Método que comprueba que el símbolo de la unidad sea válido.
    /// </summary>
    /// <param name="symbol">
    /// Símbolo que se va a validar.
    /// </param>
    /// <returns>
    /// El símbolo validado y sin espacios exteriores.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el símbolo está vacío o supera
    /// la longitud máxima.
    /// </exception>
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
