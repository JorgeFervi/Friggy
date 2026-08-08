using Friggy.Application.Catalogs.Ingredients.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class IngredientRepository(FriggyDbContext context) : IIngredientRepository
{
    public async Task<IReadOnlyList<Ingredient>> ListAsync(CancellationToken cancellationToken) =>
        await context.Ingredients.AsNoTracking().ToListAsync(cancellationToken);
    public Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Ingredients.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) =>
        context.Ingredients.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken) => await context.Ingredients.AddAsync(ingredient, cancellationToken);
    public void Remove(Ingredient ingredient) => context.Ingredients.Remove(ingredient);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
