namespace Friggy.Web.Components;

public sealed record QuantityComparisonItem(
    string Ingredient,
    string Unit,
    decimal Required,
    decimal Available,
    decimal Missing);
