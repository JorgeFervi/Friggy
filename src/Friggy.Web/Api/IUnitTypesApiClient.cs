using Friggy.Application.Catalogs.UnitTypes.Dtos;

namespace Friggy.Web.Api;

public interface IUnitTypesApiClient
{
    Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken);
    Task<UnitTypeResponse> CreateAsync(CreateUnitTypeRequest request, CancellationToken cancellationToken);
    Task<UnitTypeResponse> UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
