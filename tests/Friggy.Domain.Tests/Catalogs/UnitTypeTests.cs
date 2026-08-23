using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Tests.Catalogs;

public sealed class UnitTypeTests
{
    [Fact]
    public void Create_BlankSymbol_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(() => UnitType.Create("Gramo", " "));

        Assert.Equal("unit-type.symbol.required", exception.Code);
    }

    [Fact]
    public void Create_ValidValues_TrimsValues()
    {
        var unit = UnitType.Create(" Gramo ", " g ");

        Assert.Equal("Gramo", unit.Name.Value);
        Assert.Equal("g", unit.Symbol);
    }

    [Fact]
    public void Update_ValidValues_ChangesNameAndSymbol()
    {
        var unit = UnitType.Create("Gramo", "g");

        unit.Update("Kilogramo", "kg");

        Assert.Equal("Kilogramo", unit.Name.Value);
        Assert.Equal("kg", unit.Symbol);
    }

    [Fact]
    public void Create_Defaults_PreserveExactLegacyBehavior()
    {
        var unit = UnitType.Create("Vaso", "vaso");

        Assert.Equal(MeasurementDimension.Unconverted, unit.MeasurementDimension);
        Assert.Equal(1m, unit.BaseUnitFactor);
        Assert.True(unit.CanUseForCooking);
        Assert.True(unit.CanUseForShopping);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveBaseFactor_Throws(decimal factor)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            UnitType.Create("Peso", "p", MeasurementDimension.Mass, factor, true, true));

        Assert.Equal("unit-type.base-factor.positive", exception.Code);
    }

    [Fact]
    public void Create_UnconvertedWithDifferentFactor_Throws()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            UnitType.Create("Vaso", "vaso", MeasurementDimension.Unconverted, 250m, true, true));

        Assert.Equal("unit-type.unconverted-factor.invalid", exception.Code);
    }
}
