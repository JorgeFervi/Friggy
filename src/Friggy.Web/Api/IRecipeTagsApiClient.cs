using Friggy.Application.Catalogs.RecipeTags.Dtos;

namespace Friggy.Web.Api;

public interface IRecipeTagsApiClient
{
    Task<IReadOnlyList<RecipeTagResponse>> ListAsync(CancellationToken cancellationToken);
    Task<RecipeTagResponse> CreateAsync(CreateRecipeTagRequest request, CancellationToken cancellationToken);
    Task<RecipeTagResponse> UpdateAsync(Guid id, UpdateRecipeTagRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
