using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MessagePackFormatterGenerator {
    public interface ITypeFormatter {
        string           TypeString    { get; }
        string           FormatterName { get; }
        string           Namespace     { get; }
        INamedTypeSymbol TypeSymbol    { get; }
        TypeKind         TypeKind      { get; }
        string           FileName      { get; }
        SourceText       GenerateSource();
    }
}