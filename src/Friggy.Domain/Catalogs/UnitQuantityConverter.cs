namespace Friggy.Domain.Catalogs;

public static class UnitQuantityConverter
{
    public static bool AreCompatible(UnitType source, UnitType target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        return source.MeasurementDimension is MeasurementDimension.Unconverted
            ? source.Id == target.Id
            : source.MeasurementDimension == target.MeasurementDimension;
    }

    public static decimal ToBase(decimal quantity, UnitType unit)
    {
        ValidateQuantity(quantity);
        ArgumentNullException.ThrowIfNull(unit);
        return quantity * unit.BaseUnitFactor;
    }

    public static decimal FromBase(decimal baseQuantity, UnitType unit)
    {
        ValidateQuantity(baseQuantity);
        ArgumentNullException.ThrowIfNull(unit);
        return baseQuantity / unit.BaseUnitFactor;
    }

    public static decimal Convert(decimal quantity, UnitType source, UnitType target)
    {
        if (!AreCompatible(source, target))
        {
            throw new DomainValidationException(
                "unit-conversion.incompatible",
                "Las unidades no pertenecen a la misma dimensión.");
        }

        return FromBase(ToBase(quantity, source), target);
    }

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity < 0)
        {
            throw new DomainValidationException(
                "unit-conversion.quantity.non-negative",
                "La cantidad no puede ser negativa.");
        }
    }
}
