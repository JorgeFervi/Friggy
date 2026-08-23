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
    private static readonly Guid CookingOnlyUnitTypeId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    [Fact]
    [Trait("Category", "Component")]
    public void Inventory_UnitSelector_ShowsOnlyShoppingUnits()
    {
        RegisterApis();

        var component = Render<global::Friggy.Web.Components.Pages.Inventory>();
        var selector = component.WaitForElement("#inventory-unit");

        Assert.Contains(UnitTypeId.ToString(), selector.InnerHtml);
        Assert.DoesNotContain(CookingOnlyUnitTypeId.ToString(), selector.InnerHtml);
    }

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
    public void Inventory_ShowUnavailable_RendersResponsiveCollectionAndEveryStatus()
    {
        var api = RegisterApis();
        api.Items.Add(CreateLot(2m));
        api.Items.Add(CreateLot(0m));
        api.Items.Add(CreateLot(1m) with { IsExpired = true });
        var component = Render<global::Friggy.Web.Components.Pages.Inventory>();
        component.WaitForElement("input[type='checkbox']");

        component.Find("input[type='checkbox']").Change(true);

        component.WaitForAssertion(() =>
        {
            Assert.Equal([false, true], api.ListRequests);
            Assert.Equal(3, component.FindAll("[data-testid='inventory-lot-item']").Count);
            Assert.NotNull(component.Find(".friggy-inventory__cards"));
            Assert.NotNull(component.Find(".friggy-inventory__table"));
            Assert.Contains("Disponible", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Agotado", component.Markup, StringComparison.Ordinal);
            Assert.Contains("Caducado", component.Markup, StringComparison.Ordinal);
        });
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
            Assert.Contains("quedaron", component.Find(".friggy-inventory-details__operation-status").TextContent);
            Assert.Contains("sin registrar", component.Find(".friggy-inventory-details__operation-status").TextContent);
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
            .Single(button => button.TextContent.Trim() == "Corregir caducidad")
            .Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(new DateOnly(2026, 8, 30), api.LastExpiration?.ExpirationDate);
            Assert.Equal("Caducidad corregida.", component.Find(".friggy-inventory-details__operation-status").TextContent);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void InventoryLotDetails_Discard_RequiresOwnConfirmationAndRegistersMovement()
    {
        SetupConfirmDialog();
        var api = RegisterApis();
        var lot = CreateLot(3m);
        api.Items.Add(lot);
        var component = Render<global::Friggy.Web.Components.Pages.InventoryLotDetails>(
            parameters => parameters.Add(page => page.Id, lot.Id));
        component.WaitForElement("#lot-operation-quantity");

        component.Find("#lot-operation-quantity").Change("1");
        component.FindAll("button").Single(button => button.TextContent.Trim() == "Descartar").Click();

        Assert.Null(api.LastDiscard);
        var dialog = component.Find("dialog");
        Assert.Equal("Confirmar descarte", dialog.QuerySelector("h2")?.TextContent);
        dialog.QuerySelectorAll("button")
            .Single(button => button.TextContent == "Descartar lote")
            .Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(1m, api.LastDiscard?.Quantity);
            Assert.Equal("Descarte registrado.", component.Find(".friggy-inventory-details__operation-status").TextContent);
        });
    }

    [Fact]
    [Trait("Category", "Component")]
    public void InventoryLotDetails_Adjust_UsesSecondaryActionAndUpdatesQuantity()
    {
        var api = RegisterApis();
        var lot = CreateLot(3m);
        api.Items.Add(lot);
        var component = Render<global::Friggy.Web.Components.Pages.InventoryLotDetails>(
            parameters => parameters.Add(page => page.Id, lot.Id));
        component.WaitForElement("#lot-operation-quantity");

        component.Find("#lot-operation-quantity").Change("2.25");
        component.FindAll("button")
            .Single(button => button.TextContent.Trim() == "Ajustar a cantidad real")
            .Click();

        component.WaitForAssertion(() =>
        {
            Assert.Equal(2.25m, api.LastAdjustment?.ActualQuantity);
            Assert.Equal("Cantidad ajustada.", component.Find(".friggy-inventory-details__operation-status").TextContent);
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

    private void SetupConfirmDialog()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
    }

    private sealed class StubInventoryApiClient : IInventoryApiClient
    {
        public List<InventoryLotResponse> Items { get; } = [];
        public List<CreateInventoryLotRequest> Created { get; } = [];
        public List<bool> ListRequests { get; } = [];
        public Exception? ListException { get; set; }
        public InventoryQuantityRequest? LastConsumption { get; private set; }
        public InventoryQuantityRequest? LastDiscard { get; private set; }
        public AdjustInventoryLotRequest? LastAdjustment { get; private set; }
        public CorrectInventoryExpirationRequest? LastExpiration { get; private set; }

        public Task<IReadOnlyList<InventoryLotResponse>> ListAsync(
            bool includeUnavailable,
            CancellationToken cancellationToken)
        {
            ListRequests.Add(includeUnavailable);
            return ListException is null
                ? Task.FromResult<IReadOnlyList<InventoryLotResponse>>(
                    includeUnavailable
                        ? Items
                        : Items.Where(item => !item.IsExpired && item.Quantity > 0).ToArray())
                : Task.FromException<IReadOnlyList<InventoryLotResponse>>(ListException);
        }

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
        public Task<InventoryOperationResponse> DiscardAsync(Guid id, InventoryQuantityRequest request, CancellationToken cancellationToken)
        {
            LastDiscard = request;
            var index = Items.FindIndex(item => item.Id == id);
            var applied = Math.Min(Items[index].Quantity, request.Quantity);
            var updated = Items[index] with { Quantity = Items[index].Quantity - applied };
            Items[index] = updated;
            return Task.FromResult(new InventoryOperationResponse(
                updated,
                applied,
                request.Quantity - applied));
        }
        public Task<InventoryLotResponse> AdjustAsync(Guid id, AdjustInventoryLotRequest request, CancellationToken cancellationToken)
        {
            LastAdjustment = request;
            var index = Items.FindIndex(item => item.Id == id);
            Items[index] = Items[index] with { Quantity = request.ActualQuantity };
            return Task.FromResult(Items[index]);
        }
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
            Task.FromResult<IReadOnlyList<UnitTypeResponse>>([
                new(UnitTypeId, "Kilogramo", "kg"),
                new UnitTypeResponse(CookingOnlyUnitTypeId, "Cucharada", "cda")
                {
                    MeasurementDimension = "volume",
                    BaseUnitFactor = 15m,
                    CanUseForCooking = true,
                    CanUseForShopping = false,
                },
            ]);
        public Task<UnitTypeResponse> CreateAsync(CreateUnitTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<UnitTypeResponse> UpdateAsync(Guid id, UpdateUnitTypeRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
