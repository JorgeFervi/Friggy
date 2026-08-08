using Friggy.Application.Catalogs.Ingredients.Dtos;

namespace Friggy.Web.Api;

public interface IIngredientsApiClient
{
    Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken);
    Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken);
    Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
