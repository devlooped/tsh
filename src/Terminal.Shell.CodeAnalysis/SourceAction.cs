using Microsoft.CodeAnalysis;

namespace Terminal.Shell.CodeAnalysis;

abstract class SourceAction
{
    public static SymbolDisplayFormat FileNameFormat { get; } = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
    public static SymbolDisplayFormat FullNameFormat { get; } = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public abstract void Execute();
}