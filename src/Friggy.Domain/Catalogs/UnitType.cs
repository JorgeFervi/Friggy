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
        BaseUnitFactor = 1m;
        CanUseForCooking = true;
        CanUseForShopping = true;
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
    private UnitType(
        Guid id,
        CatalogName name,
        string symbol,
        MeasurementDimension measurementDimension,
        decimal baseUnitFactor,
        bool canUseForCooking,
        bool canUseForShopping)
    {
        Id = id;
        Name = name;
        Symbol = symbol;
        MeasurementDimension = measurementDimension;
        BaseUnitFactor = baseUnitFactor;
        CanUseForCooking = canUseForCooking;
        CanUseForShopping = canUseForShopping;
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

    public MeasurementDimension MeasurementDimension { get; private set; }

    public decimal BaseUnitFactor { get; private set; }

    public bool CanUseForCooking { get; private set; }

    public bool CanUseForShopping { get; private set; }

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
        Create(
            name,
            symbol,
            MeasurementDimension.Unconverted,
            1m,
            canUseForCooking: true,
            canUseForShopping: true);

    public static UnitType Create(
        string? name,
        string? symbol,
        MeasurementDimension measurementDimension,
        decimal baseUnitFactor,
        bool canUseForCooking,
        bool canUseForShopping) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "unit-type.name.required"),
            ValidateSymbol(symbol),
            ValidateMeasurementDimension(measurementDimension),
            ValidateBaseUnitFactor(measurementDimension, baseUnitFactor),
            canUseForCooking,
            canUseForShopping);

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

    public void Update(
        string? name,
        string? symbol,
        MeasurementDimension measurementDimension,
        decimal baseUnitFactor,
        bool canUseForCooking,
        bool canUseForShopping)
    {
        Name = CatalogName.Create(name, "unit-type.name.required");
        Symbol = ValidateSymbol(symbol);
        MeasurementDimension = ValidateMeasurementDimension(measurementDimension);
        BaseUnitFactor = ValidateBaseUnitFactor(measurementDimension, baseUnitFactor);
        CanUseForCooking = canUseForCooking;
        CanUseForShopping = canUseForShopping;
    }

    private static MeasurementDimension ValidateMeasurementDimension(
        MeasurementDimension measurementDimension)
    {
        if (!Enum.IsDefined(measurementDimension))
        {
            throw new DomainValidationException(
                "unit-type.measurement-dimension.invalid",
                "La dimensión de medida no es válida.");
        }

        return measurementDimension;
    }

    private static decimal ValidateBaseUnitFactor(
        MeasurementDimension measurementDimension,
        decimal baseUnitFactor)
    {
        if (baseUnitFactor <= 0)
        {
            throw new DomainValidationException(
                "unit-type.base-factor.positive",
                "El factor base debe ser positivo.");
        }

        if (measurementDimension is MeasurementDimension.Unconverted && baseUnitFactor != 1m)
        {
            throw new DomainValidationException(
                "unit-type.unconverted-factor.invalid",
                "Una unidad sin conversión debe usar factor 1.");
        }

        return baseUnitFactor;
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
