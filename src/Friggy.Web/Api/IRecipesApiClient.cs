using Friggy.Application.Recipes.Dtos;

namespace Friggy.Web.Api;

public interface IRecipesApiClient
{
    Task<IReadOnlyList<RecipeListItemResponse>> ListAsync(CancellationToken cancellationToken);
    Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<RecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken);
    Task<RecipeResponse> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
