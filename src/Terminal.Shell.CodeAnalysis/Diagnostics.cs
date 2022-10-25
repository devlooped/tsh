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
}
