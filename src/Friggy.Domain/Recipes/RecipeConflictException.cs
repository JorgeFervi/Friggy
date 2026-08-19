namespace Friggy.Domain.Recipes;

/// <summary>
/// Clase <see cref="RecipeConflictException"/> que representa un conflicto
/// al aplicar una regla de identidad o unicidad de una receta.
/// </summary>
/// <param name="code">
/// Código de error.
/// </param>
/// <param name="message">
/// Mensaje descriptivo asociado al error.
/// </param>
public sealed class RecipeConflictException(string code, string message)
    : Exception(message)
{
    /// <summary>
    /// Código que identifica la regla de dominio incumplida.
    /// </summary>
    public string Code { get; } = code;
}
