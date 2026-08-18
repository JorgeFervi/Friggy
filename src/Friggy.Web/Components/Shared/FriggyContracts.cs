namespace Friggy.Web.Components.DesignSystem;

public enum FriggyButtonVariant
{
    Primary,
    Secondary,
    Danger,
    Ghost,
}

public enum FriggyButtonType
{
    Button,
    Submit,
    Reset,
}

public enum FriggyButtonSize
{
    Regular,
    Compact,
}

public enum FriggyTone
{
    Neutral,
    Info,
    Success,
    Warning,
    Danger,
}

public enum FriggyIconName
{
    Home,
    Recipe,
    Calendar,
    Inventory,
    Catalogs,
    Menu,
    Close,
    Add,
    Edit,
    Delete,
    Back,
    Time,
}

public sealed record FormFieldContext(
    string ControlId,
    string? DescribedBy,
    bool Invalid);
