using Microsoft.CodeAnalysis;

namespace Terminal.Shell;

public static class Diagnostics
{
    public static DiagnosticDescriptor MenuCommandShouldBeAttributed { get; } =
        new DiagnosticDescriptor(
            "TSH0001",
            "IMenuCommand implementations should be annotated with [MenuCommand]",
            "Menu command class '{0}' should be annotated with the [MenuCommand] attribute",
            "Terminal.Shell",
            DiagnosticSeverity.Warning,
            true);

    public static DiagnosticDescriptor MenuCommandClassMustBePartial { get; } =
        new DiagnosticDescriptor(
            "TSH0002",
            "Menu command class must be partial",
            "Menu command class '{0}' must be partial",
            "Terminal.Shell",
            DiagnosticSeverity.Error,
            true);

    public static DiagnosticDescriptor MenuCommandMethodMustBeVisible { get; } =
        new DiagnosticDescriptor(
            "TSH0003",
            "Menu command method must be visible",
            "Menu command method '{0}' must be internal or public",
            "Terminal.Shell",
            DiagnosticSeverity.Error,
            true);

    public static DiagnosticDescriptor MenuCommandMethodClassMustBeVisible { get; } =
        new DiagnosticDescriptor(
            "TSH0004",
            "Menu command method declaring type must be visible",
            "Declaring type '{0}' of menu command method '{1}' must be internal or public",
            "Terminal.Shell",
            DiagnosticSeverity.Error,
            true);

    public static DiagnosticDescriptor ThemeMustBeColorScheme { get; } =
        new DiagnosticDescriptor(
            "TSH0005",
            "Theme must be of type Terminal.Gui.ColorScheme",
            "Theme '{0}' must be of type Terminal.Gui.ColorScheme",
            "Terminal.Shell",
            DiagnosticSeverity.Error,
            true);

    public static DiagnosticDescriptor ExportedClassMustBePartial { get; } =
        new DiagnosticDescriptor(
            "TSH0005",
            "Exported classes must be partial",
            "Exported class '{0}' must be partial",
            "Terminal.Shell",
            DiagnosticSeverity.Error,
            true);
}
