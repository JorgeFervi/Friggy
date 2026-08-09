namespace Friggy.Application.Recipes.Interfaces;

public interface IRecipeCatalogRepository
{
    Task<bool> IngredientsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> UnitTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> TagsExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<bool> MealTypesExistAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
}
