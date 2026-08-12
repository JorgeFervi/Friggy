using Bunit;
using Friggy.Application.Catalogs.Ingredients.Dtos;
using Friggy.Application.Catalogs.UnitTypes.Dtos;
using Friggy.Application.Inventory.Dtos;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Friggy.ComponentTests.Inventory;

public sealed class InventoryPageTests : ComponentTest
{
    private static readonly Guid IngredientId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Category", "Component")]
    public void Inventory_EmptyState_ShowsFormAndMessage()
    {
        var api = RegisterApis();

        var component = Render<global::Friggy.Web.Components.Pages.Inventory>();

        component.WaitForAssertion(() =>
        {
            Assert.NotNull(component.Find("form[data-testid='inventory-lot-form']"));
            Assert.Contains("No hay lotes", component.Markup, StringComparison.Ordinal);
            Assert.Empty(api.Created);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Inventory_ValidForm_CreatesLotAndRefreshesList()
    {
        var api = RegisterApis();
        var component = Render<global::Friggy.Web.Components.Pages.Inventory>();
        component.WaitForElement("#inventory-ingredient");

        component.Find("#inventory-ingredient").Change(IngredientId.ToString());
        component.Find("#inventory-unit").Change(UnitTypeId.ToString());
        component.Find("#inventory-quantity").Change("2.5");
        component.Find("#inventory-expiration").Change("2026-08-20");
        component.Find("form[data-testid='inventory-lot-form']").Submit();

        component.WaitForAssertion(() =>
        {
            var request = Assert.Single(api.Created);
            Assert.Equal(2.5m, request.Quantity);
            Assert.Contains("Tomate", component.Markup, StringComparison.Ordinal);
            Assert.Contains("kg", component.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void Inventory_ApiFailure_ShowsErrorState()
    {
        var api = RegisterApis();
        api.ListException = new HttpRequestException("API no disponible");

        var component = Render<global::Friggy.Web.Components.Pages.Inventory>();

        component.WaitForAssertion(() =>
            Assert.Contains("API no disponible", component.Find("[role='alert']").TextContent));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void InventoryLotDetails_PartialConsumption_ShowsAppliedAndUnappliedConfirmation()
    {
        var api = RegisterApis();
        var lot = CreateLot(1.5m);
        api.Items.Add(lot);
        var component = Render<global::Friggy.Web.Components.Pages.InventoryLotDetails>(
            parameters => parameters.Add(page => page.Id, lot.Id));
        component.WaitForElement("#lot-operation-quantity");

        component.Find("#lot-operation-quantity").Change("2");
        component.FindAll("button").Single(button => button.TextContent == "Consumir").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2m, api.LastConsumption?.Quantity);
            Assert.Contains("quedaron", component.Find("[role='status']").TextContent);
            Assert.Contains("sin registrar", component.Find("[role='status']").TextContent);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void InventoryLotDetails_CorrectExpiration_UpdatesThroughHttpClientContract()
    {
        var api = RegisterApis();
        var lot = CreateLot(1m);
        api.Items.Add(lot);
        var component = Render<global::Friggy.Web.Components.Pages.InventoryLotDetails>(
            parameters => parameters.Add(page => page.Id, lot.Id));
        component.WaitForElement("#lot-expiration-date");

        component.Find("#lot-expiration-date").Change("2026-08-30");
        component.FindAll("button")
            .Single(button => button.TextContent == "Corregir caducidad")
            .Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(new DateOnly(2026, 8, 30), api.LastExpiration?.ExpirationDate);
            Assert.Equal("Caducidad corregida.", component.Find("[role='status']").TextContent);
        });
    }

    private static InventoryLotResponse CreateLot(decimal quantity) =>
        new(
            Guid.NewGuid(),
            IngredientId,
            "Tomate",
            UnitTypeId,
            "Kilogramo",
            "kg",
            quantity,
            new DateOnly(2026, 8, 20),
            false,
            []);

    private StubInventoryApiClient RegisterApis()
    {
        var api = new StubInventoryApiClient();
        Services.AddSingleton<IInventoryApiClient>(api);
        Services.AddSingleton<IIngredientsApiClient>(new StubIngredientsApiClient());
        Services.AddSingleton<IUnitTypesApiClient>(new StubUnitTypesApiClient());
        return api;
    }

    private sealed class StubInventoryApiClient : IInventoryApiClient
    {
        public List<InventoryLotResponse> Items { get; } = [];
        public List<CreateInventoryLotRequest> Created { get; } = [];
        public Exception? ListException { get; set; }
        public InventoryQuantityRequest? LastConsumption { get; private set; }
        public CorrectInventoryExpirationRequest? LastExpiration { get; private set; }

        public Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
            bool includeUnavailable,
            CancellationToken cancellationToken) =>
            ListException is null
                ? Task.FromResult<IReadOnlyList<InventoryLotResponse>>(Items)
                : Task.FromException<IReadOnlyList<InventoryLotResponse>>(ListException);

        public Task<InventoryLotResponse> CreateAsync(
            CreateInventoryLotRequest request,
            CancellationToken cancellationToken)
        {
            Created.Add(request);
            var response = new InventoryLotResponse(
                Guid.NewGuid(),
                request.IngredientId,
                "Tomate",
                request.UnitTypeId,
                "Kilogramo",
                "kg",
                request.Quantity,
                request.ExpirationDate,
                false,
                []);
            Items.Add(response);
            return Task.FromResult(response);
        }

        public Task<InventoryLotResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Single(item => item.Id == id));
        public Task<InventoryLotResponse> CorrectExpirationAsync(Guid id, CorrectInventoryExpirationRequest request, CancellationToken cancellationToken)
        {
            LastExpiration = request;
            var index = Items.FindIndex(item => item.Id == id);
            Items[index] = Items[index] with { ExpirationDate = request.ExpirationDate };
            return Task.FromResult(Items[index]);
        }
        public Task<InventoryOperationResponse> ConsumeAsync(Guid id, InventoryQuantityRequest request, CancellationToken cancellationToken)
        {
            LastConsumption = request;
            var index = Items.FindIndex(item => item.Id == id);
            var applied = Math.Min(Items[index].Quantity, request.Quantity);
            var updated = Items[index] with { Quantity = Items[index].Quantity - applied };
            Items[index] = updated;
            return Task.FromResult(new InventoryOperationResponse(
                updated,
                applied,
                request.Quantity - applied));
        }
        public Task<InventoryOperationResponse> DiscardAsync(Guid id, InventoryQuantityRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<InventoryLotResponse> AdjustAsync(Guid id, AdjustInventoryLotRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubIngredientsApiClient : IIngredientsApiClient
    {
        public Task<IReadOnlyList<IngredientResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IngredientResponse>>([new(IngredientId, "Tomate")]);
        public Task<IngredientResponse> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<IngredientResponse> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubUnitTypesApiClient : IUnitTypesApiClient
    {
        public Task<IReadOnlyList<UnitTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UnitTypeResponse>>([new(UnitTypeId, "Kilogramo", "kg")]);
        public Task<UnitTypeResponse> CreateAsync(CreateUnitTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<UnitTypeResponse> UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
