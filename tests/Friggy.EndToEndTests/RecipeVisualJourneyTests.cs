using Friggy.EndToEndTests.Testing;
using Microsoft.Playwright;

namespace Friggy.EndToEndTests;

[Collection(FullStackTestGroup.Name)]
public sealed class RecipeVisualJourneyTests(FullStackFixture fixture) : FriggyPageTest
{
    private static readonly ViewportSize Mobile = new() { Width = 360, Height = 800 };
    private static readonly ViewportSize Desktop = new() { Width = 1440, Height = 1000 };

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "Visual")]
    public async Task RecipePages_EmptyFormPopulatedHomeAndDetail_MatchResponsiveBaselines()
    {
        const string ingredientName = "Tomate visual recetas";
        const string recipeName = "Gazpacho visual recetas";
        var recipeCreated = false;
        var visualFailures = new List<Exception>();

        await RunScenarioAsync(async () =>
        {
            try
            {
                await fixture.ResetRecipeDataAsync(TestContext.Current.CancellationToken);
                await NavigateToInteractivePageAsync("/ingredients");
                await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(ingredientName);
                await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir", Exact = true }).ClickAsync();
                await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = ingredientName, Exact = true })).ToBeVisibleAsync();

                await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
                await NavigateToInteractivePageAsync("/recipes/new");
                await CaptureAsync("recipe-form-mobile-360x800", visualFailures);
                await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
                await CaptureAsync("recipe-form-desktop-1440x1000", visualFailures);

                await Page.GetByLabel("Nombre", new() { Exact = true }).FillAsync(recipeName);
                await Page.GetByLabel("Tiempo estimado (minutos)", new() { Exact = true }).FillAsync("20");
                await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
                await Page.GetByLabel("Ingrediente", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
                await Page.GetByLabel("Unidad", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
                await Page.GetByLabel("Cantidad", new() { Exact = true }).FillAsync("1.5");
                await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir paso", Exact = true }).ClickAsync();
                await Page.GetByLabel("Descripción", new() { Exact = true }).FillAsync("Triturar y servir frío");
                await Page.GetByLabel("Comida", new() { Exact = true }).CheckAsync();
                await Page.GetByRole(AriaRole.Button, new() { Name = "Guardar receta", Exact = true }).ClickAsync();
                await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = recipeName, Exact = true })).ToBeVisibleAsync();
                recipeCreated = true;

                var ingredientsSection = Page.GetByRole(
                    AriaRole.Region,
                    new() { Name = "Ingredientes", Exact = true });
                var stepsSection = Page.GetByRole(
                    AriaRole.Region,
                    new() { Name = "Pasos", Exact = true });
                var ingredientsBox = await ingredientsSection
                    .GetByRole(AriaRole.Heading, new() { Name = "Ingredientes", Exact = true })
                    .BoundingBoxAsync();
                var stepsBox = await stepsSection
                    .GetByRole(AriaRole.Heading, new() { Name = "Pasos", Exact = true })
                    .BoundingBoxAsync();
                Assert.NotNull(ingredientsBox);
                Assert.NotNull(stepsBox);
                Assert.InRange(Math.Abs(ingredientsBox.Y - stepsBox.Y), 0, 1);

                await CaptureAsync("recipe-detail-desktop-1440x1000", visualFailures);
                await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
                await CaptureAsync("recipe-detail-mobile-360x800", visualFailures);

                await Page.GetByRole(AriaRole.Link, new() { Name = "Editar receta", Exact = true }).ClickAsync();
                await Page.Locator("[data-testid='interactive-ready']").WaitForAsync(
                    new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
                for (var index = 2; index <= 8; index++)
                {
                    await Page.GetByRole(AriaRole.Button, new() { Name = "Añadir ingrediente", Exact = true }).ClickAsync();
                    var ingredientRow = Page.Locator("[data-testid='ingredient-row']").Nth(index - 1);
                    await ingredientRow.GetByLabel("Ingrediente", new() { Exact = true })
                        .SelectOptionAsync(new SelectOptionValue { Label = ingredientName });
                    await ingredientRow.GetByLabel("Unidad", new() { Exact = true })
                        .SelectOptionAsync(new SelectOptionValue { Label = "Gramo (g)" });
                    await ingredientRow.GetByLabel("Cantidad", new() { Exact = true })
                        .FillAsync(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    await ingredientRow.GetByRole(
                            AriaRole.Button,
                            new() { Name = $"Terminar edición del ingrediente {index}", Exact = true })
                        .ClickAsync();
                }

                await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
                await CaptureAsync("recipe-edit-eight-ingredients-desktop-1440x1000", visualFailures);
                await Page.SetViewportSizeAsync(Mobile.Width, Mobile.Height);
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
                await CaptureAsync("recipe-edit-eight-ingredients-mobile-360x800", visualFailures);

                await NavigateToInteractivePageAsync("/");
                await CaptureAsync("home-recipes-mobile-360x800", visualFailures);
                await Page.SetViewportSizeAsync(Desktop.Width, Desktop.Height);
                await CaptureAsync("home-recipes-desktop-1440x1000", visualFailures);

                if (visualFailures.Count > 0)
                {
                    throw new AggregateException("Las capturas visuales requieren revisión.", visualFailures);
                }
            }
            finally
            {
                if (recipeCreated)
                {
                    await NavigateToInteractivePageAsync("/recipes");
                    var card = Page.Locator("article[data-recipe-id]").Filter(new LocatorFilterOptions { HasText = recipeName });
                    await card.GetByRole(AriaRole.Button, new() { Name = "Borrar", Exact = true }).ClickAsync();
                    await Page.GetByRole(AriaRole.Dialog, new() { Name = "Borrar receta", Exact = true })
                        .GetByRole(AriaRole.Button, new() { Name = "Borrar receta", Exact = true })
                        .ClickAsync();
                }
            }
        });
    }

    private async Task CaptureAsync(string name, List<Exception> failures)
    {
        try
        {
            await VisualSnapshot.AssertAsync(Page, name, TestContext.Current.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            failures.Add(exception);
        }
    }
}
