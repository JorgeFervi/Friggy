namespace Friggy.Domain.Catalogs;

public sealed class Ingredient
{
    private Ingredient()
    {
        Name = null!;
    }

    private Ingredient(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public static Ingredient Create(string? name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "ingredient.name.required"));

    public void Rename(string? name) =>
        Name = CatalogName.Create(name, "ingredient.name.required");
}
