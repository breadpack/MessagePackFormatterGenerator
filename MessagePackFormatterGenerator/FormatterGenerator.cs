using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;

namespace MessagePackFormatterGenerator {
    [Generator]
    public class FormatterGenerator : ISourceGenerator {
        public void Initialize(GeneratorInitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new ClassStructDeclarationReceiver());
        }

        public void Execute(GeneratorExecutionContext context) {
            if (context.SyntaxReceiver is not ClassStructDeclarationReceiver receiver) {
                return;
            }
            
            // if (!Debugger.IsAttached)
            //     Debugger.Launch();

            var compilation                = context.Compilation;
            var messagePackAttributeSymbol = compilation.GetTypeByMetadataName(AttributeNames.MessagePackObject);
            if (messagePackAttributeSymbol == null) {
                return;
            }

            var classSymbols  = FindAttribetedClasses(receiver, compilation, messagePackAttributeSymbol);
            var structSymbols = FindAttributedStructs(receiver, compilation, messagePackAttributeSymbol);
            var allSymbols    = classSymbols.Concat(structSymbols).ToArray();

            var allTypes =
                allSymbols.Concat(
                              allSymbols.SelectMany(t => t.GetRelatedTypes(true))
                          )
                          .Distinct()
                          .ToArray();

            // 현재 컨텍스트에 포함된 타입들만 필터링
            var formatters = allTypes
                                .Where(t => GetTypesInAssembly(compilation.Assembly).Contains(t))
                                .Select(t => new TypeFormatter(t))
                                .Cast<ITypeFormatter>()
                                .ToArray();
            
            if(formatters.Length == 0) {
                return;
            }

            formatters = formatters.Concat(
                                       formatters.Where(f => f.TypeKind == TypeKind.Struct)
                                                 .Select(f => new NullableTypeFormatter(f.TypeSymbol))
                                   )
                                   .ToArray();

            foreach (var type in formatters) {
                context.AddSource(type.FileName, type.GenerateSource());
            }

            context.AddSource("FormatterResolver.g.cs", GenerateResolver(context, formatters));
        }

        private SourceText GenerateResolver(GeneratorExecutionContext context, ITypeFormatter[] formatters) {
            var sb = new StringBuilder();
            sb.AppendLine("using MessagePack;");
            sb.AppendLine("using MessagePack.Formatters;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System;");
            foreach (var @namespace in formatters.Select(t => t.Namespace).Distinct()) {
                sb.AppendLine($"using {@namespace};");
            }

            sb.AppendLine();
            
            // root namespace is common and shortest namespace for all formatters
            var rootNamespace = formatters.Select(t => t.Namespace)
                                          .Aggregate((common, current) => {
                                              var minLength = Math.Min(common.Length, current.Length);
                                              var commonLength = common.Take(minLength)
                                                                      .TakeWhile((c, i) => c == current[i])
                                                                      .Count();
                                              return common.Substring(0, commonLength);
                                          });
            
            if(!string.IsNullOrEmpty(rootNamespace)) {
                sb.AppendLine($"namespace {rootNamespace} {{");
            }
            
            
            sb.AppendLine("    public class FormatterResolver : IFormatterResolver {");
            sb.AppendLine("        public static readonly FormatterResolver Instance = new FormatterResolver();");
            sb.AppendLine("        private readonly Dictionary<Type, object> _formatters;");
            sb.AppendLine();
            sb.AppendLine("        private FormatterResolver() {");
            sb.AppendLine("            _formatters = new () {");

            foreach (var formatterSymbol in formatters) {
                sb.AppendLine($"                {{ typeof({formatterSymbol.TypeString}), new {formatterSymbol.FormatterName}() }},");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public IMessagePackFormatter<T> GetFormatter<T>() {");
            sb.AppendLine("            if (_formatters.TryGetValue(typeof(T), out var formatter)) {");
            sb.AppendLine("                return (IMessagePackFormatter<T>)formatter;");
            sb.AppendLine("            }");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(rootNamespace)) {
                sb.AppendLine("}");
            }
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private IEnumerable<INamedTypeSymbol> FindAttribetedClasses(ClassStructDeclarationReceiver receiver, Compilation compilation, INamedTypeSymbol messagePackAttributeSymbol) {
            return
                from classDeclaration in receiver.CandidateClasses
                let model = compilation.GetSemanticModel(classDeclaration.SyntaxTree)
                let classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol
                where classSymbol?.HasAttribute(messagePackAttributeSymbol, true) == true
                select classSymbol;
        }

        private IEnumerable<INamedTypeSymbol> FindAttributedStructs(ClassStructDeclarationReceiver receiver, Compilation compilation, INamedTypeSymbol messagePackAttributeSymbol) {
            return
                from structDeclaration in receiver.CandidateStructs
                let model = compilation.GetSemanticModel(structDeclaration.SyntaxTree)
                let structSymbol = model.GetDeclaredSymbol(structDeclaration) as INamedTypeSymbol
                where structSymbol?.HasAttribute(messagePackAttributeSymbol, true) == true
                select structSymbol;
        }

        private static HashSet<INamedTypeSymbol> GetTypesInAssembly(IAssemblySymbol assemblySymbol) {
            var result = new HashSet<INamedTypeSymbol>();

            void CollectTypes(INamespaceSymbol ns) {
                foreach (var member in ns.GetMembers()) {
                    switch (member) {
                        case INamedTypeSymbol typeSymbol:
                            result.Add(typeSymbol);
                            break;
                        case INamespaceSymbol nestedNs:
                            CollectTypes(nestedNs);
                            break;
                    }
                }
            }

            CollectTypes(assemblySymbol.GlobalNamespace);
            return result;
        }
    }
}