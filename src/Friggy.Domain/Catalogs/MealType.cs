namespace Friggy.Domain.Catalogs;

/// <summary>
/// Clase <see cref="MealType"/> que representa un tipo de comida disponible
/// para clasificar recetas y planificar comidas.
/// </summary>
public sealed class MealType
{
    /// <summary>
    /// Constructor vacío que usa EF Core antes de asignar los
    /// valores de las propiedades de forma especial.
    /// </summary>
    private MealType()
    {
        Name = null!;
    }

    /// <summary>
    /// Constructor usado por los métodos <see cref="Create"/> y
    /// <see cref="Update"/>.
    /// </summary>
    /// <param name="id">
    /// Código único para identificar al tipo de comida de forma interna.
    /// </param>
    /// <param name="name">
    /// Nombre del tipo de comida.
    /// </param>
    /// <param name="order">
    /// Orden de presentación del tipo de comida.
    /// </param>
    private MealType(Guid id, CatalogName name, int order)
    {
        Id = id;
        Name = name;
        Order = order;
    }

    /// <summary>
    /// Código único para identificar al tipo de comida de forma interna.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Nombre del tipo de comida.
    /// </summary>
    public CatalogName Name { get; private set; }

    /// <summary>
    /// Orden de presentación del tipo de comida.
    /// </summary>
    public int Order { get; private set; }

    /// <summary>
    /// Constructor público principal.
    /// </summary>
    /// <param name="name">
    /// Nombre del tipo de comida.
    /// </param>
    /// <param name="order">
    /// Orden de presentación del tipo de comida.
    /// </param>
    /// <returns>
    /// Objeto <see cref="MealType"/>.
    /// </returns>
    public static MealType Create(string? name, int order) =>
        new(
            Guid.NewGuid(),
            CatalogName.Create(name, "meal-type.name.required"),
            ValidateOrder(order));

    /// <summary>
    /// Método para cambiar el nombre y el orden del tipo de comida.
    /// </summary>
    /// <param name="name">
    /// Nuevo nombre que reemplazará al anterior.
    /// </param>
    /// <param name="order">
    /// Nuevo orden de presentación.
    /// </param>
    public void Update(string? name, int order)
    {
        Name = CatalogName.Create(name, "meal-type.name.required");
        Order = ValidateOrder(order);
    }

    /// <summary>
    /// Método que comprueba que el orden sea válido.
    /// </summary>
    /// <param name="order">
    /// Orden que se va a validar.
    /// </param>
    /// <returns>
    /// El orden validado.
    /// </returns>
    /// <exception cref="DomainValidationException">
    /// Excepción de dominio lanzada cuando el orden es negativo.
    /// </exception>
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
