using Friggy.Application.Inventory.Dtos;

namespace Friggy.Web.Api;

public interface IInventoryApiClient
{
    Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
        bool includeUnavailable,
        CancellationToken cancellationToken);

    Task<InventoryLotResponse> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<InventoryLotResponse> CreateAsync(
        CreateInventoryLotRequest request,
        CancellationToken cancellationToken);

    Task<InventoryLotResponse> CorrectExpirationAsync(
        Guid id,
        CorrectInventoryExpirationRequest request,
        CancellationToken cancellationToken);

    Task<InventoryOperationResponse> ConsumeAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken);

    Task<InventoryOperationResponse> DiscardAsync(
        Guid id,
        InventoryQuantityRequest request,
        CancellationToken cancellationToken);

    Task<InventoryLotResponse> AdjustAsync(
        Guid id,
        AdjustInventoryLotRequest request,
        CancellationToken cancellationToken);
}
