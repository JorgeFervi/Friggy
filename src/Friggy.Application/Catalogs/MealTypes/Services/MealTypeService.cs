using Friggy.Application.Catalogs.MealTypes.Dtos;
using Friggy.Application.Catalogs.MealTypes.Interfaces;
using Friggy.Domain.Catalogs;

namespace Friggy.Application.Catalogs.MealTypes.Services;

public sealed class MealTypeService(IMealTypeRepository repository)
{
    public async Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).OrderBy(x => x.Order).ThenBy(x => x.Name.Value, StringComparer.CurrentCultureIgnoreCase).Select(Map).ToArray();

    public async Task<MealTypeResponse> GetAsync(Guid id, CancellationToken cancellationToken) => Map(await FindAsync(id, cancellationToken));

    public async Task<MealTypeResponse> CreateAsync(CreateMealTypeRequest request, CancellationToken cancellationToken)
    {
        var item = MealType.Create(request.Name, request.Order);
        await EnsureUniqueAsync(item.Name.Normalized, null, cancellationToken);
        await repository.AddAsync(item, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<MealTypeResponse> UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        item.Update(request.Name, request.Order);
        await EnsureUniqueAsync(item.Name.Normalized, item.Id, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await FindAsync(id, cancellationToken);
        repository.Remove(item);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<MealType> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new CatalogNotFoundException("meal-type.not-found", "No se encontró el tipo de comida.");

    private async Task EnsureUniqueAsync(string name, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (await repository.ExistsByNormalizedNameAsync(name, excludingId, cancellationToken))
        {
            throw new CatalogConflictException("meal-type.name.duplicate", "Ya existe un tipo de comida con ese nombre.");
        }
    }

    private static MealTypeResponse Map(MealType item) => new(item.Id, item.Name.Value, item.Order);
}
