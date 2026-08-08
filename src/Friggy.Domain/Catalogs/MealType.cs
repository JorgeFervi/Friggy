namespace Friggy.Domain.Catalogs;

public sealed class MealType
{
    private MealType()
    {
        Name = null!;
    }

    private MealType(Guid id, CatalogName name, int order)
    {
        Id = id;
        Name = name;
        Order = order;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public int Order { get; private set; }

    public static MealType Create(string? name, int order) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "meal-type.name.required"),
            ValidateOrder(order));

    public void Update(string? name, int order)
    {
        Name = CatalogName.Create(name, "meal-type.name.required");
        Order = ValidateOrder(order);
    }

    private static int ValidateOrder(int order)
    {
        if (order < 0)
        {
            throw new DomainValidationException(
                "meal-type.order.non-negative",
                "El orden no puede ser negativo.");
        }

        return order;
    }
}
