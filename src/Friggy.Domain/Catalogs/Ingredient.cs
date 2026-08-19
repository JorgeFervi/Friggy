namespace Friggy.Domain.Catalogs;

/// <summary>
/// Clase <see cref="Ingredient"/> que representa un ingrediente usado en uno o
/// varios pasos de una receta.
/// </summary>
public sealed class Ingredient
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private Ingredient()
    {
        Name = null!;
    }

    /// <summary>
    /// Constructor usado por el método <see cref="Create"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al ingrediente de forma
    /// interna.
    /// </param>
    /// <param name="name">
    /// Nombre del ingrediente.
    /// </param>
    private Ingredient(Guid id, CatalogName name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Código único para identificar al ingrediente de forma
    /// interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre del ingrediente.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre del ingrediente.
    /// </param>
    /// <returns>
    /// Objeto <see cref="Ingredient"/>.
    /// </returns>
    public static Ingredient Create(string? name) =>
        new(Guid.NewGuid(), CatalogName.Create(name, "ingredient.name.required"));

    /// <summary>
    /// Método para cambiar el nombre del ingrediente.
    /// </summary>
    /// <param name="name">
    /// Nuevo nombre que reemplazrá al anterior.
    /// </param>
    public void Rename(string? name) =>
        Name = CatalogName.Create(name, "ingredient.name.required");
}
