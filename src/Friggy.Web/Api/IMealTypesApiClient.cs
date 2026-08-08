using Friggy.Application.Catalogs.MealTypes.Dtos;

namespace Friggy.Web.Api;

public interface IMealTypesApiClient
{
    Task<IReadOnlyList<MealTypeResponse>> ListAsync(CancellationToken cancellationToken);
    Task<MealTypeResponse> CreateAsync(CreateMealTypeRequest request, CancellationToken cancellationToken);
    Task<MealTypeResponse> UpdateAsync(Guid id, UpdateMealTypeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
