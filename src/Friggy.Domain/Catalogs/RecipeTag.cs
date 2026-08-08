namespace Friggy.Domain.Catalogs;

public sealed class RecipeTag
{
    private RecipeTag()
    {
        Name = null!;
    }

    private RecipeTag(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public CatalogName Name { get; private set; }

    public static RecipeTag Create(string? name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "recipe-tag.name.required"));

    public void Rename(string? name) =>
        Name = CatalogName.Create(name, "recipe-tag.name.required");
}
