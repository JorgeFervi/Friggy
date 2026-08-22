using Bunit;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace Friggy.ComponentTests.Layout;

public sealed class MainLayoutTests : ComponentTest
{
    [Fact]
    [Trait("Category", "Component")]
    public void Render_FromRecipesPage_ShowsGroupedNavigationAndActiveRoute()
    {
        var navigation = GetRequiredService<NavigationManager>();
        navigation.NavigateTo("recipes");

        var component = Render<MainLayout>();

        Assert.EndsWith("/recipes", navigation.Uri, StringComparison.Ordinal);
        Assert.Equal("Saltar al contenido", component.Find("a[href='#main-content']").TextContent.Trim());

        var sidebar = component.Find("aside[data-testid='desktop-sidebar']");
        Assert.Equal("Navegación de Friggy", sidebar.GetAttribute("aria-label"));
        Assert.Collection(
            sidebar.QuerySelectorAll("nav[aria-label='Principal'] a"),
            link => AssertNavigationLink(link, "Inicio", "/", active: false),
            link => AssertNavigationLink(link, "Recetas", "/recipes", active: true),
            link => AssertNavigationLink(link, "Planes diarios", "/daily-plans", active: false),
            link => AssertNavigationLink(link, "Plantillas", "/daily-plan-templates", active: false),
            link => AssertNavigationLink(link, "Lista de la compra", "/shopping-list", active: false),
            link => AssertNavigationLink(link, "Inventario", "/inventory", active: false));
        Assert.Equal("Planificación", sidebar.QuerySelector("[aria-label='Planificación']")?.ParentElement?.QuerySelector("h2")?.TextContent.Trim());
        Assert.Equal("Catálogos", sidebar.QuerySelector("[data-navigation-group='catalogs'] h2")?.TextContent.Trim());
        Assert.Collection(
            sidebar.QuerySelectorAll("[data-navigation-group='catalogs'] a"),
            link => AssertNavigationLink(link, "Ingredientes", "/ingredients", active: false),
            link => AssertNavigationLink(link, "Unidades", "/unit-types", active: false),
            link => AssertNavigationLink(link, "Etiquetas", "/recipe-tags", active: false),
            link => AssertNavigationLink(link, "Tipos de comida", "/meal-types", active: false));
        Assert.Equal("main-content", component.Find("main").Id);
        Assert.Equal("-1", component.Find("main").GetAttribute("tabindex"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MobileMenu_OpenAndOverlayClose_ExposeAccessibleDrawerState()
    {
        var module = JavaScript.SetupModule("./js/navigation-drawer.js");
        module.SetupVoid("open", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        var component = Render<MainLayout>();

        var trigger = component.Find("button[aria-controls='navigation-drawer']");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal("Abrir menú", trigger.GetAttribute("aria-label"));

        trigger.Click();

        Assert.Equal("true", component.Find("button[aria-controls='navigation-drawer']").GetAttribute("aria-expanded"));
        Assert.Equal("false", component.Find("#navigation-drawer").GetAttribute("aria-hidden"));
        Assert.False(component.Find("#navigation-drawer").HasAttribute("inert"));
        Assert.Contains("friggy-navigation-drawer--open", component.Find("#navigation-drawer").ClassList);
        module.VerifyInvoke("open");

        component.Find("[data-testid='drawer-overlay']").Click();

        Assert.Equal("false", component.Find("button[aria-controls='navigation-drawer']").GetAttribute("aria-expanded"));
        Assert.Equal("true", component.Find("#navigation-drawer").GetAttribute("aria-hidden"));
        Assert.True(component.Find("#navigation-drawer").HasAttribute("inert"));
        module.VerifyInvoke("close");
    }

    [Fact]
    [Trait("Category", "Component")]
    public void MobileMenu_CloseButtonEscapeAndNavigation_CloseDrawerExactlyOnce()
    {
        var module = JavaScript.SetupModule("./js/navigation-drawer.js");
        module.SetupVoid("open", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        var component = Render<MainLayout>();

        component.Find("button[aria-controls='navigation-drawer']").Click();
        component.Find("#navigation-drawer button[aria-label='Cerrar menú']").Click();
        Assert.Equal("true", component.Find("#navigation-drawer").GetAttribute("aria-hidden"));

        component.Find("button[aria-controls='navigation-drawer']").Click();
        component.Find("#navigation-drawer").KeyDown("Escape");
        Assert.Equal("true", component.Find("#navigation-drawer").GetAttribute("aria-hidden"));

        component.Find("button[aria-controls='navigation-drawer']").Click();
        component.Find("#navigation-drawer a[href='/recipes']").Click();
        Assert.Equal("true", component.Find("#navigation-drawer").GetAttribute("aria-hidden"));
        module.VerifyInvoke("open", 3);
        module.VerifyInvoke("close", 3);
    }

    private static void AssertNavigationLink(
        AngleSharp.Dom.IElement link,
        string text,
        string href,
        bool active)
    {
        Assert.Equal(text, link.TextContent.Trim());
        Assert.Equal(href, link.GetAttribute("href"));
        Assert.Equal(active ? "page" : null, link.GetAttribute("aria-current"));
    }
}
