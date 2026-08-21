using Bunit;
using Friggy.ComponentTests.Testing;
using Friggy.Web.Components.DesignSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Friggy.ComponentTests.DesignSystem;

public sealed class FriggyPrimitivesTests : ComponentTest
{
    [Theory]
    [InlineData(FriggyButtonVariant.Primary, "friggy-button--primary")]
    [InlineData(FriggyButtonVariant.Secondary, "friggy-button--secondary")]
    [InlineData(FriggyButtonVariant.Danger, "friggy-button--danger")]
    [InlineData(FriggyButtonVariant.Ghost, "friggy-button--ghost")]
    [Trait("Category", "Component")]
    public void FriggyButton_VariantAndType_RendersClosedContract(
        FriggyButtonVariant variant,
        string expectedClass)
    {
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.Variant, variant)
            .Add(p => p.Type, FriggyButtonType.Submit)
            .AddChildContent("Guardar"));

        var button = component.Find("button");

        Assert.Equal("submit", button.GetAttribute("type"));
        Assert.Contains(expectedClass, button.ClassList);
        Assert.Equal("Guardar", button.TextContent.Trim());
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FriggyButton_DisabledOrBusy_PreventsEventsAndExposesState()
    {
        var clickCount = 0;
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.Busy, true)
            .Add(p => p.BusyLabel, "Guardando")
            .Add(p => p.OnClick, () => clickCount++)
            .AddChildContent("Guardar"));

        component.Find("button").Click();

        var button = component.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.Equal("Guardando", button.TextContent.Trim());
        Assert.Equal(0, clickCount);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FriggyButton_Click_PropagatesExactlyOnce()
    {
        var clickCount = 0;
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.OnClick, () => clickCount++)
            .AddChildContent("Añadir"));

        component.Find("button").Click();

        Assert.Equal(1, clickCount);
    }

    [Theory]
    [InlineData(FriggyButtonType.Button, "button")]
    [InlineData(FriggyButtonType.Submit, "submit")]
    [InlineData(FriggyButtonType.Reset, "reset")]
    [Trait("Category", "Component")]
    public void FriggyButton_EachType_RendersNativeButtonType(FriggyButtonType type, string expected)
    {
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.Type, type)
            .AddChildContent("Acción"));

        Assert.Equal(expected, component.Find("button").GetAttribute("type"));
    }

    [Theory]
    [InlineData(FriggyButtonSize.Regular, "friggy-button--regular")]
    [InlineData(FriggyButtonSize.Compact, "friggy-button--compact")]
    [Trait("Category", "Component")]
    public void FriggyButton_EachSize_RendersClosedVariant(FriggyButtonSize size, string expectedClass)
    {
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.Size, size)
            .AddChildContent("Acción"));

        Assert.Contains(expectedClass, component.Find("button").ClassList);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FriggyButton_Disabled_PreventsEventWithoutBusyState()
    {
        var clickCount = 0;
        var component = Render<FriggyButton>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.OnClick, () => clickCount++)
            .AddChildContent("Borrar"));

        component.Find("button").Click();

        Assert.Equal(0, clickCount);
        Assert.Null(component.Find("button").GetAttribute("aria-busy"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FriggyIconButton_LabelAndIcon_HaveAccessibleSemantics()
    {
        var component = Render<FriggyIconButton>(parameters => parameters
            .Add(p => p.Label, "Editar receta")
            .Add(p => p.Icon, FriggyIconName.Edit));

        var button = component.Find("button");
        Assert.Equal("Editar receta", button.GetAttribute("aria-label"));
        Assert.Equal("Editar receta", button.GetAttribute("title"));
        Assert.Equal("true", button.QuerySelector("svg")?.GetAttribute("aria-hidden"));
    }

    [Theory]
    [InlineData(FriggyIconName.Home)]
    [InlineData(FriggyIconName.Recipe)]
    [InlineData(FriggyIconName.Calendar)]
    [InlineData(FriggyIconName.Inventory)]
    [InlineData(FriggyIconName.Catalogs)]
    [InlineData(FriggyIconName.Menu)]
    [InlineData(FriggyIconName.Close)]
    [InlineData(FriggyIconName.Add)]
    [InlineData(FriggyIconName.Edit)]
    [InlineData(FriggyIconName.Delete)]
    [InlineData(FriggyIconName.Back)]
    [InlineData(FriggyIconName.Time)]
    [Trait("Category", "Component")]
    public void FriggyIcon_EachLocalIcon_RendersDecorativeSvg(FriggyIconName icon)
    {
        var component = Render<FriggyIcon>(parameters => parameters.Add(p => p.Name, icon));

        var svg = component.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.False(string.IsNullOrWhiteSpace(component.Find("path").GetAttribute("d")));
    }

    [Theory]
    [InlineData(FriggyTone.Neutral, "status-badge--neutral")]
    [InlineData(FriggyTone.Info, "status-badge--info")]
    [InlineData(FriggyTone.Success, "status-badge--success")]
    [InlineData(FriggyTone.Warning, "status-badge--warning")]
    [InlineData(FriggyTone.Danger, "status-badge--danger")]
    [Trait("Category", "Component")]
    public void StatusBadge_EachTone_RendersClosedVariant(FriggyTone tone, string expectedClass)
    {
        var component = Render<StatusBadge>(parameters => parameters
            .Add(p => p.Tone, tone)
            .AddChildContent("Estado"));

        Assert.Contains(expectedClass, component.Find("span").ClassList);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ContentPrimitives_RenderSemanticStructureAndContent()
    {
        var card = Render<FriggyCard>(parameters => parameters
            .Add(p => p.Heading, "Receta")
            .Add(p => p.Footer, "Acciones")
            .AddChildContent("Contenido"));
        var header = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Mis recetas")
            .Add(p => p.Description, "Organiza tus platos")
            .Add(p => p.Actions, "Crear receta"));
        var badge = Render<StatusBadge>(parameters => parameters
            .Add(p => p.Tone, FriggyTone.Success)
            .AddChildContent("Disponible"));
        var feedback = Render<FeedbackPanel>(parameters => parameters
            .Add(p => p.Tone, FriggyTone.Danger)
            .Add(p => p.Title, "No se pudo guardar")
            .AddChildContent("Inténtalo de nuevo"));

        Assert.Equal("Receta", card.Find("article h2").TextContent.Trim());
        Assert.Equal("Contenido", card.Find(".friggy-card__content").TextContent.Trim());
        Assert.Equal("Acciones", card.Find("footer").TextContent.Trim());
        Assert.Equal("Mis recetas", header.Find("h1").TextContent.Trim());
        Assert.Equal("Organiza tus platos", header.Find("p").TextContent.Trim());
        Assert.Equal("Crear receta", header.Find(".page-header__actions").TextContent.Trim());
        Assert.Equal("status", badge.Find("span").GetAttribute("role"));
        Assert.Contains("status-badge--success", badge.Find("span").ClassList);
        Assert.Equal("alert", feedback.Find("section").GetAttribute("role"));
        Assert.Equal("No se pudo guardar", feedback.Find("h2").TextContent.Trim());
    }

    [Fact]
    [Trait("Category", "Component")]
    public void CompositionPrimitives_RenderLabelledSectionBreadcrumbAndFormActions()
    {
        var section = Render<PageSection>(parameters => parameters
            .Add(p => p.Heading, "Recetas recientes")
            .Add(p => p.Description, "Tus últimos platos")
            .Add(p => p.Actions, "Ver todas")
            .AddChildContent("Galería"));
        var header = Render<PageHeader>(parameters => parameters
            .Add(p => p.Title, "Editar receta")
            .Add(p => p.Breadcrumb, "Recetas / Editar"));
        var actions = Render<FormActions>(parameters => parameters.AddChildContent("Guardar Cancelar"));

        var heading = section.Find("h2");
        Assert.Equal(heading.Id, section.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal("Galería", section.Find(".page-section__content").TextContent.Trim());
        Assert.Equal("Ver todas", section.Find(".page-section__actions").TextContent.Trim());
        Assert.Equal("Migas de pan", header.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("Guardar Cancelar", actions.Find(".form-actions").TextContent.Trim());
    }

    [Fact]
    [Trait("Category", "Component")]
    public void FormField_LabelHelpAndError_AreAssociatedWithoutDuplicateIds()
    {
        var first = RenderField("Nombre", "Tu nombre público", null);
        var second = RenderField("Tiempo", null, "Introduce un tiempo válido");

        var firstInput = first.Find("input");
        var secondInput = second.Find("input");
        var firstId = firstInput.Id;
        var secondId = secondInput.Id;

        Assert.NotEqual(firstId, secondId);
        Assert.Equal(firstId, first.Find("label").GetAttribute("for"));
        Assert.Equal(secondId, second.Find("label").GetAttribute("for"));
        Assert.Equal(first.Find(".form-field__help").Id, firstInput.GetAttribute("aria-describedby"));
        Assert.Equal(second.Find(".form-field__error").Id, secondInput.GetAttribute("aria-describedby"));
        Assert.Equal("true", secondInput.GetAttribute("aria-invalid"));
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ConfirmDialog_OpenConfirmAndCancel_ManageFocusAndEmitOnce()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        var confirmations = 0;
        var cancellations = 0;
        var component = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.Title, "Borrar receta")
            .Add(p => p.Message, "Esta acción no se puede deshacer.")
            .Add(p => p.OnConfirm, () => confirmations++)
            .Add(p => p.OnCancel, () => cancellations++));

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));
        component.Render(parameters => parameters.Add(p => p.IsOpen, true));
        module.VerifyInvoke("show");
        Assert.Equal("dialog", component.Find("dialog").GetAttribute("role"));
        Assert.Equal("true", component.Find("dialog").GetAttribute("aria-modal"));
        Assert.True(component.Find(".confirm-dialog__actions button.friggy-button--secondary").HasAttribute("autofocus"));

        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();
        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        Assert.Equal(1, confirmations);
        Assert.Equal(0, cancellations);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));
        component.Render(parameters => parameters.Add(p => p.IsOpen, true));
        component.Find(".confirm-dialog__actions button.friggy-button--secondary").Click();
        Assert.Equal(1, cancellations);
        module.VerifyInvoke("close", 2);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ConfirmDialog_Confirm_ClosesBeforeNotifyingParent()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        var closedBeforeConfirmation = false;
        var component = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Borrar receta")
            .Add(p => p.Message, "Esta acción no se puede deshacer.")
            .Add(
                p => p.OnConfirm,
                () => closedBeforeConfirmation = module.Invocations.Identifiers.Contains("close")));

        component.Find(".confirm-dialog__actions button.friggy-button--danger").Click();

        Assert.True(closedBeforeConfirmation);
    }

    [Fact]
    [Trait("Category", "Component")]
    public void ConfirmDialog_Cancel_ClosesBeforeNotifyingParent()
    {
        var module = JavaScript.SetupModule("./js/confirm-dialog.js");
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();
        var closedBeforeCancellation = false;
        var component = Render<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.Title, "Borrar receta")
            .Add(p => p.Message, "Esta acción no se puede deshacer.")
            .Add(
                p => p.OnCancel,
                () => closedBeforeCancellation = module.Invocations.Identifiers.Contains("close")));

        component.Find(".confirm-dialog__actions button.friggy-button--secondary").Click();

        Assert.True(closedBeforeCancellation);
    }

    private IRenderedComponent<FormField> RenderField(string label, string? help, string? error) =>
        Render<FormField>(parameters => parameters
            .Add(p => p.Label, label)
            .Add(p => p.Help, help)
            .Add(p => p.Error, error)
            .Add(p => p.ChildContent, context => builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "id", context.ControlId);
                builder.AddAttribute(2, "aria-describedby", context.DescribedBy);
                builder.AddAttribute(3, "aria-invalid", context.Invalid ? "true" : null);
                builder.CloseElement();
            }));
}
