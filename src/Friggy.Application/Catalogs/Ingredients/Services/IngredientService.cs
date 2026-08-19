using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.Ingredients.Services;

/// <summary>
/// Servicio de aplicación que gestiona el ciclo de vida de los ingredientes.
/// </summary>
public sealed class IngredientService(IIngredientRepository repository)
{
    /// <summary>
    /// Obtiene los ingredientes ordenados por nombre.
    /// </summary>
    public async Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken))
            .OrderBy(item => item.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Map)
            .ToArray();

    /// <summary>
    /// Obtiene un ingrediente por su identificador.
    /// </summary>
    public async Task<IngredientResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await FindAsync(id, cancellationToken));

    /// <summary>
    /// Crea un ingrediente y persiste sus cambios.
    /// </summary>
    public async Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken)
    {
        var ingredient = Ingredient.Create(request.Name);
        await EnsureUniqueAsync(ingredient.Name.Normalized, null, cancellationToken);
        await repository.AddAsync(ingredient, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(ingredient);
    }

    /// <summary>
    /// Actualiza un ingrediente y persiste sus cambios.
    /// </summary>
    public async Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken)
    {
        var ingredient = await FindAsync(id, cancellationToken);
        ingredient.Rename(request.Name);
        await EnsureUniqueAsync(ingredient.Name.Normalized, ingredient.Id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(ingredient);
    }

    /// <summary>
    /// Elimina un ingrediente y persiste sus cambios.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ingredient = await FindAsync(id, cancellationToken);
        repository.Remove(ingredient);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ingredient> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ??
        throw new CatalogNotFoundException("ingredient.not-found", "No se encontró el ingrediente.");

    private async Task EnsureUniqueAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNormalizedNameAsync(normalizedName, excludingId, cancellationToken))
        {
            throw new CatalogConflictException("ingredient.name.duplicate", "Ya existe un ingrediente con ese nombre.");
        }
    }

    private static IngredientResponse Map(Ingredient item) => new(item.Id, item.Name.Value);
}
