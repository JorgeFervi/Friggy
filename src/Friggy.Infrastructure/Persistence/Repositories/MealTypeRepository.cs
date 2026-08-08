using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class MealTypeRepository(FriggyDbContext context) : IMealTypeRepository
{
    public async Task<IReadOnlyList<MealType>> ListAsync(CancellationToken cancellationToken) => await context.MealTypes.AsNoTracking().ToListAsync(cancellationToken);
    public Task<MealType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.MealTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.MealTypes.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    public async Task AddAsync(MealType item, CancellationToken cancellationToken) => await context.MealTypes.AddAsync(item, cancellationToken);
    public void Remove(MealType item) => context.MealTypes.Remove(item);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
