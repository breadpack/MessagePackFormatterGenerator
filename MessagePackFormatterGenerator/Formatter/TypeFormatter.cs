using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MessagePackFormatterGenerator {
    public partial class TypeFormatter : ITypeFormatter {
        private readonly ISymbol[] Members;

        public TypeFormatter(INamedTypeSymbol typeSymbol) {
            TypeSymbol = typeSymbol;

            Members = TypeSymbol.GetMembers()
                                .Where(m => m.Kind is SymbolKind.Field
                                                   or SymbolKind.Property)
                                .Where(m => (
                                                m.DeclaredAccessibility == Accessibility.Public
                                             && m.GetAttributes()
                                                 .All(a => a.AttributeClass.ToDisplayString() != AttributeNames.JsonIgnore
                                                        && a.AttributeClass.ToDisplayString() != AttributeNames.UnityJsonIgnore
                                                        && a.AttributeClass.ToDisplayString() != AttributeNames.IgnoreMember
                                                        && a.AttributeClass.ToDisplayString() != AttributeNames.NonSerialized)
                                            )
                                         || (
                                                m.DeclaredAccessibility == Accessibility.Private
                                             && m.GetAttributes()
                                                 .Any(a => a.AttributeClass.ToDisplayString() == AttributeNames.SerializeField
                                                        || a.AttributeClass.ToDisplayString() == AttributeNames.JsonProperty
                                                        || a.AttributeClass.ToDisplayString() == AttributeNames.UnityJsonProperty)
                                            )
                                )
                                .Where(m => m switch {
                                    IFieldSymbol field => !field.IsConst
                                                       && !field.IsStatic
                                                       && !field.IsImplicitlyDeclared,
                                    IPropertySymbol property => property.SetMethod != null,
                                    _                        => true
                                })
                                .ToArray();
        }

        public TypeKind TypeKind => TypeSymbol.TypeKind;


        public INamedTypeSymbol TypeSymbol    { get; }
        public string           TypeString    => TypeSymbol.ToDisplayString();
        public string           FormatterName => TypeSymbol.Name + "Formatter";

        public string FileName
            => TypeSymbol.ToDisplayString()
                         .Replace("<", "[")
                         .Replace(">", "]")
                         .Replace(",", ".")
             + ".Formatter.g.cs";

        public string Namespace
            => !string.IsNullOrWhiteSpace(TypeSymbol.ContainingNamespace?.Name)
                   ? TypeSymbol.ContainingNamespace?.ToDisplayString()
                   : string.Empty;

        public SourceText GenerateSource() {
            var sb = new StringBuilder();
            BuildUsings(sb);

            BeginNamespace(sb);
            {
                BeginClass(sb);
                {
                    BuildSerializeMethod(sb);
                    BuildDeserializeMethod(sb);
                }
                EndClass(sb);
            }
            EndNamespace(sb);

            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }
    }
}