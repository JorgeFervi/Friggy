using Friggy.Application.Recipes.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core que comprueba la existencia de referencias de catálogo
/// usadas por las recetas.
/// </summary>
public sealed class RecipeCatalogRepository(FriggyDbContext context)
    : IRecipeCatalogRepository
{
    /// <summary>
    /// Comprueba que existan todos los ingredientes indicados.
    /// </summary>
    public Task<bool> IngredientsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.Ingredients.Select(item => item.Id),
            ids,
            cancellationToken);

    /// <summary>
    /// Comprueba que existan todas las unidades indicadas.
    /// </summary>
    public Task<bool> UnitTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.UnitTypes.Select(item => item.Id),
            ids,
            cancellationToken);

    /// <summary>
    /// Obtiene las unidades solicitadas junto con sus capacidades de uso.
    /// </summary>
    public async Task<IReadOnlyList<RecipeUnitTypeReference>> GetUnitTypesAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        return await context.UnitTypes
            .AsNoTracking()
            .Where(item => distinctIds.Contains(item.Id))
            .Select(item => new RecipeUnitTypeReference(item.Id, item.CanUseForCooking))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Comprueba que existan todas las etiquetas indicadas.
    /// </summary>
    public Task<bool> TagsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.RecipeTags.Select(item => item.Id),
            ids,
            cancellationToken);

    /// <summary>
    /// Comprueba que existan todos los tipos de comida indicados.
    /// </summary>
    public Task<bool> MealTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        AllExistAsync(
            context.MealTypes.Select(item => item.Id),
            ids,
            cancellationToken);

    private static async Task<bool> AllExistAsync(
        IQueryable<Guid> availableIds,
        IReadOnlyCollection<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = requestedIds.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return true;
        }

        var existingCount = await availableIds.CountAsync(
            id => distinctIds.Contains(id),
            cancellationToken);
        return existingCount == distinctIds.Length;
    }
}
