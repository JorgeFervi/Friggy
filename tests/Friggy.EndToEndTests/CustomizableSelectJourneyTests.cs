using Friggy.EndToEndTests.Testing;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class CustomizableSelectJourneyTests : FriggyPageTest
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task InventorySelect_SupportedBrowser_UsesCustomizablePickerStates()
    {
        await RunScenarioAsync(async () =>
        {
            await NavigateToInteractivePageAsync("/inventory");
            var ingredientSelect = Page.GetByLabel("Ingrediente", new() { Exact = true });

            Assert.True(await Page.EvaluateAsync<bool>(
                "() => CSS.supports('appearance', 'base-select')"));
            Assert.Equal(
                "base-select",
                await ingredientSelect.EvaluateAsync<string>(
                    "element => getComputedStyle(element).appearance"));
            Assert.Equal(
                "base-select",
                await ingredientSelect.EvaluateAsync<string>(
                    "element => getComputedStyle(element, '::picker(select)').appearance"));

            await ingredientSelect.ClickAsync();

            Assert.True(await ingredientSelect.EvaluateAsync<bool>(
                "element => element.matches(':open')"));
            var selectHandle = await ingredientSelect.ElementHandleAsync();
            Assert.NotNull(selectHandle);
            await Page.WaitForFunctionAsync(
                "element => getComputedStyle(element, '::picker-icon').rotate === '180deg'",
                selectHandle);
            Assert.Equal(
                "180deg",
                await ingredientSelect.EvaluateAsync<string>(
                    "element => getComputedStyle(element, '::picker-icon').rotate"));
            Assert.Equal(
                "flex",
                await ingredientSelect.Locator("option:checked").EvaluateAsync<string>(
                    "element => getComputedStyle(element).display"));

            await Page.Keyboard.PressAsync("Escape");
            Assert.False(await ingredientSelect.EvaluateAsync<bool>(
                "element => element.matches(':open')"));
        });
    }
}
