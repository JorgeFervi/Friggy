using Friggy.Application.Catalogs.RecipeTags.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class RecipeTagRepository(FriggyDbContext context) : IRecipeTagRepository
{
    public async Task<IReadOnlyList<RecipeTag>> ListAsync(CancellationToken cancellationToken) => await context.RecipeTags.AsNoTracking().ToListAsync(cancellationToken);
    public Task<RecipeTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.RecipeTags.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.RecipeTags.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    public async Task AddAsync(RecipeTag item, CancellationToken cancellationToken) => await context.RecipeTags.AddAsync(item, cancellationToken);
    public void Remove(RecipeTag item) => context.RecipeTags.Remove(item);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
