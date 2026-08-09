using Friggy.Application.Recipes.Interfaces;
using Friggy.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class RecipeRepository(FriggyDbContext context) : IRecipeRepository
{
    public async Task<IReadOnlyList<Recipe>> ListAsync(
        CancellationToken cancellationToken) =>
        await CompleteQuery()
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync(cancellationToken);

    public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        CompleteQuery().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        context.Recipes.AnyAsync(
            item => item.Name.Normalized == normalizedName &&
                (!excludingId.HasValue || item.Id != excludingId.Value),
            cancellationToken);

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken) =>
        await context.Recipes.AddAsync(recipe, cancellationToken);

    public void Remove(Recipe recipe) => context.Recipes.Remove(recipe);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);

    private IQueryable<Recipe> CompleteQuery() =>
        context.Recipes
            .Include(recipe => recipe.Ingredients.OrderBy(item => item.Order))
            .Include(recipe => recipe.Steps.OrderBy(item => item.Order))
            .Include(recipe => recipe.Tags)
            .Include(recipe => recipe.MealTypes)
            .AsSplitQuery();
}
