using Friggy.Application.Recipes.Dtos;
using Friggy.Application.Recipes.Interfaces;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio EF Core para consultar y persistir recetas con sus asociaciones.
/// </summary>
public sealed class RecipeRepository(FriggyDbContext context) : IRecipeRepository
{
    /// <summary>
    /// Obtiene todas las recetas completas con sus relaciones.
    /// </summary>
    public async Task<IReadOnlyList<Recipe>> ListAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Obtiene los resúmenes de recetas para listados.
    /// </summary>
    public async Task<IReadOnlyList<RecipeListItemResponse>> ListSummariesAsync(
        CancellationToken cancellationToken)
    {
        var items = await context.Recipes
            .AsNoTracking()
            .OrderBy(recipe => recipe.Name.Normalized)
            .Select(recipe => new
            {
                recipe.Id,
                Name = recipe.Name.Value,
                recipe.EstimatedTime,
            })
            .ToListAsync(cancellationToken);

        return items
            .Select(item => new RecipeListItemResponse(
                item.Id,
                item.Name,
                checked((int)item.EstimatedTime.TotalMinutes)))
            .ToArray();
    }

    /// <summary>
    /// Busca una receta completa por su identificador.
    /// </summary>
    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    /// <summary>
    /// Comprueba si existe una receta con el nombre normalizado indicado.
    /// </summary>
    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        context.Recipes.AnyAsync(
            item => item.Name.Normalized == normalizedName &&
                (!excludingId.HasValue || item.Id != excludingId.Value),
            cancellationToken);

    /// <summary>
    /// Añade una receta al contexto.
    /// </summary>
    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken) =>
        await context.Recipes.AddAsync(recipe, cancellationToken);

    /// <summary>
    /// Marca una receta para eliminarla.
    /// </summary>
    public void Remove(Recipe recipe) => context.Recipes.Remove(recipe);

    /// <summary>
    /// Persiste los cambios pendientes.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);

    private IQueryable<Recipe> CompleteQuery() =>
        context.Recipes
            .Include(recipe => recipe.Ingredients.OrderBy(item => item.Order))
            .Include(recipe => recipe.Steps.OrderBy(item => item.Order))
                .ThenInclude(step => step.IngredientLinks)
            .Include(recipe => recipe.Tags)
            .Include(recipe => recipe.MealTypes)
            .AsSplitQuery();
}
