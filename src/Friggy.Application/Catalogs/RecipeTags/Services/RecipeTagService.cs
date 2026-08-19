using Friggy.Application.Catalogs.RecipeTags.Dtos;
using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.RecipeTags.Services;

/// <summary>
/// Servicio de aplicación que gestiona el ciclo de vida de las etiquetas de receta.
/// </summary>
public sealed class RecipeTagService(IRecipeTagRepository repository)
{
    /// <summary>
    /// Obtiene las etiquetas ordenadas por nombre.
    /// </summary>
    public async Task<IReadOnlyList<RecipeTagResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).OrderBy(x => x.Name.Value, StringComparer.CurrentCultureIgnoreCase).Select(Map).ToArray();

    /// <summary>
    /// Obtiene una etiqueta por su identificador.
    /// </summary>
    public async Task<RecipeTagResponse> GetAsync(Guid id, CancellationToken cancellationToken) => Map(await FindAsync(id, cancellationToken));

    /// <summary>
    /// Crea una etiqueta y persiste sus cambios.
    /// </summary>
    public async Task<RecipeTagResponse> CreateAsync(CreateRecipeTagRequest request, CancellationToken cancellationToken)
    {
        var item = RecipeTag.Create(request.Name);
        await EnsureUniqueAsync(item.Name.Normalized, null, cancellationToken);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    /// <summary>
    /// Actualiza una etiqueta y persiste sus cambios.
    /// </summary>
    public async Task<RecipeTagResponse> UpdateAsync(Guid id, UpdateRecipeTagRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        item.Rename(request.Name);
        await EnsureUniqueAsync(item.Name.Normalized, item.Id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    /// <summary>
    /// Elimina una etiqueta y persiste sus cambios.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<RecipeTag> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new CatalogNotFoundException("recipe-tag.not-found", "No se encontró la etiqueta.");

    private async Task EnsureUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNormalizedNameAsync(name, excludingId, cancellationToken))
        {
            throw new CatalogConflictException("recipe-tag.name.duplicate", "Ya existe una etiqueta con ese nombre.");
        }
    }

    private static RecipeTagResponse Map(RecipeTag item) => new(item.Id, item.Name.Value);
}
