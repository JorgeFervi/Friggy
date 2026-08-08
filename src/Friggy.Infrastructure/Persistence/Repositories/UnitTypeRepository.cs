using Friggy.Application.Catalogs.UnitTypes.Interfaces;
using Friggy.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Friggy.Infrastructure.Persistence.Repositories;

public sealed class UnitTypeRepository(FriggyDbContext context) : IUnitTypeRepository
{
    public async Task<IReadOnlyList<UnitType>> ListAsync(CancellationToken cancellationToken) => await context.UnitTypes.AsNoTracking().ToListAsync(cancellationToken);
    public Task<UnitType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => context.UnitTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken) => context.UnitTypes.AnyAsync(item => item.Name.Normalized == normalizedName && (!excludingId.HasValue || item.Id != excludingId.Value), cancellationToken);
    public async Task AddAsync(UnitType item, CancellationToken cancellationToken) => await context.UnitTypes.AddAsync(item, cancellationToken);
    public void Remove(UnitType item) => context.UnitTypes.Remove(item);
    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await context.SaveChangesAsync(cancellationToken);
}
