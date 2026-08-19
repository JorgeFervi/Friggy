namespace Friggy.Domain.Catalogs;

/// <summary>
/// Clase <see cref="RecipeTag"/> que representa una etiqueta que puede
/// asociarse a una receta.
/// </summary>
public sealed class RecipeTag
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private RecipeTag()
    {
        Name = null!;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar a la etiqueta de forma interna.
    /// </param>
    /// <param name="name">
    /// Nombre de la etiqueta.
    /// </param>
    private RecipeTag(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Código único para identificar a la etiqueta de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre de la etiqueta.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre de la etiqueta.
    /// </param>
    /// <returns>
    /// Objeto <see cref="RecipeTag"/>.
    /// </returns>
    public static RecipeTag Create(string? name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "recipe-tag.name.required"));

    /// <summary>
    /// Método para cambiar el nombre de la etiqueta.
    /// </summary>
    /// <param name="name">
    /// Nuevo nombre que reemplazará al anterior.
    /// </param>
    public void Rename(string? name) =>
        Name = CatalogName.Create(name, "recipe-tag.name.required");
}
