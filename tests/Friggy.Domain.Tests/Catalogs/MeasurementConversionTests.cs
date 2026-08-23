using Friggy.Domain.Catalogs;

namespace Friggy.Domain.Tests.Catalogs;

public sealed class MeasurementConversionTests
{
    [Fact]
    public void Convert_KilogramsToGrams_ReturnsEquivalentQuantity()
    {
        var kilograms = UnitType.Create("Kilogramo", "kg", MeasurementDimension.Mass, 1000m, true, true);
        var grams = UnitType.Create("Gramo", "g", MeasurementDimension.Mass, 1m, true, true);

        var result = UnitQuantityConverter.Convert(1m, kilograms, grams);

        Assert.Equal(1000m, result);
    }

    [Fact]
    public void Convert_TablespoonToMilliliters_ReturnsFifteen()
    {
        var tablespoon = UnitType.Create("Cucharada", "cda", MeasurementDimension.Volume, 15m, true, false);
        var milliliters = UnitType.Create("Mililitro", "ml", MeasurementDimension.Volume, 1m, true, true);

        Assert.Equal(15m, UnitQuantityConverter.Convert(1m, tablespoon, milliliters));
    }

    [Fact]
    public void Convert_DifferentDimensions_Throws()
    {
        var grams = UnitType.Create("Gramo", "g", MeasurementDimension.Mass, 1m, true, true);
        var milliliters = UnitType.Create("Mililitro", "ml", MeasurementDimension.Volume, 1m, true, true);

        var exception = Assert.Throws<DomainValidationException>(() =>
            UnitQuantityConverter.Convert(1m, grams, milliliters));

        Assert.Equal("unit-conversion.incompatible", exception.Code);
    }

    [Fact]
    public void AreCompatible_DifferentUnconvertedUnits_ReturnsFalse()
    {
        var first = UnitType.Create("Vaso", "vaso");
        var second = UnitType.Create("Pizca", "pizca");

        Assert.False(UnitQuantityConverter.AreCompatible(first, second));
        Assert.True(UnitQuantityConverter.AreCompatible(first, first));
    }
}
