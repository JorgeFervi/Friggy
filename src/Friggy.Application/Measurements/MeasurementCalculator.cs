using Friggy.Domain.Catalogs;

namespace Friggy.Application.Measurements;

internal readonly record struct MeasuredAmount(
    Guid IngredientId,
    string IngredientName,
    Guid UnitTypeId,
    decimal Quantity);

internal readonly record struct MeasurementComparison(
    Guid IngredientId,
    string IngredientName,
    UnitType Unit,
    decimal Required,
    decimal Available,
    decimal Missing);

internal readonly record struct MeasurementBucket(
    Guid IngredientId,
    MeasurementDimension Dimension,
    Guid? ExactUnitTypeId)
{
    public static MeasurementBucket Create(Guid ingredientId, UnitType unit) => new(
        ingredientId,
        unit.MeasurementDimension,
        unit.MeasurementDimension is MeasurementDimension.Unconverted ? unit.Id : null);
}

internal static class MeasurementCalculator
{
    public static IReadOnlyList<MeasurementComparison> Compare(
        IEnumerable<MeasuredAmount> required,
        IEnumerable<MeasuredAmount> available,
        IReadOnlyCollection<UnitType> units)
    {
        var unitsById = units.ToDictionary(unit => unit.Id);
        var requiredGroups = Normalize(required, unitsById);
        var availableGroups = Normalize(available, unitsById)
            .ToDictionary(item => item.Key, item => item.Value.Quantity);

        return requiredGroups.Select(item =>
            {
                var availableBase = availableGroups.GetValueOrDefault(item.Key);
                var outputUnit = SelectOutputUnit(
                    item.Key,
                    item.Value.Quantity,
                    units);
                return new MeasurementComparison(
                    item.Key.IngredientId,
                    item.Value.IngredientName,
                    outputUnit,
                    UnitQuantityConverter.FromBase(item.Value.Quantity, outputUnit),
                    UnitQuantityConverter.FromBase(availableBase, outputUnit),
                    UnitQuantityConverter.FromBase(
                        Math.Max(0, item.Value.Quantity - availableBase),
                        outputUnit));
            })
            .ToArray();
    }

    public static bool AreCompatible(UnitType first, UnitType second) =>
        UnitQuantityConverter.AreCompatible(first, second);

    private static Dictionary<MeasurementBucket, NormalizedAmount> Normalize(
        IEnumerable<MeasuredAmount> amounts,
        Dictionary<Guid, UnitType> units)
    {
        var result = new Dictionary<MeasurementBucket, NormalizedAmount>();
        foreach (var amount in amounts)
        {
            var unit = units[amount.UnitTypeId];
            var key = MeasurementBucket.Create(amount.IngredientId, unit);
            var quantity = UnitQuantityConverter.ToBase(amount.Quantity, unit);
            var current = result.GetValueOrDefault(key);
            result[key] = new(
                string.IsNullOrEmpty(current.IngredientName)
                    ? amount.IngredientName
                    : current.IngredientName,
                current.Quantity + quantity);
        }

        return result;
    }

    private static UnitType SelectOutputUnit(
        MeasurementBucket bucket,
        decimal requiredBase,
        IReadOnlyCollection<UnitType> units)
    {
        if (bucket.Dimension is MeasurementDimension.Unconverted)
        {
            var exact = units.Single(unit => unit.Id == bucket.ExactUnitTypeId);
            if (!exact.CanUseForShopping)
            {
                throw MissingShoppingUnit();
            }

            return exact;
        }

        var candidates = units
            .Where(unit => unit.MeasurementDimension == bucket.Dimension &&
                unit.CanUseForShopping)
            .OrderByDescending(unit => unit.BaseUnitFactor)
            .ThenBy(unit => unit.Name.Normalized, StringComparer.Ordinal)
            .ThenBy(unit => unit.Id)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw MissingShoppingUnit();
        }

        return candidates.FirstOrDefault(unit => requiredBase / unit.BaseUnitFactor >= 1m)
            ?? candidates[^1];
    }

    private static DomainValidationException MissingShoppingUnit() => new(
        "unit-type.shopping-unit.required",
        "No existe una unidad de compra compatible.");

    private readonly record struct NormalizedAmount(string IngredientName, decimal Quantity);
}
