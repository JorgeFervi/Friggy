namespace Friggy.Application.Catalogs.RecipeTags.Dtos;

/// <summary>
/// Datos de respuesta de una etiqueta de receta.
/// </summary>
public sealed record RecipeTagResponse(Guid Id, string Name);
