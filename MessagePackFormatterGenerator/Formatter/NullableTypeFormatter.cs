using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MessagePackFormatterGenerator {
    public class NullableTypeFormatter : ITypeFormatter {
        public NullableTypeFormatter(INamedTypeSymbol typeSymbol) {
            // change typeSymbol to UnderlyingTypeSymbol
            UnderlyingTypeSymbol = typeSymbol;
            // create nullable type symbol
            TypeSymbol = typeSymbol.ContainingAssembly.GetTypeByMetadataName("System.Nullable`1")?.Construct(typeSymbol) ?? typeSymbol;
        }

        public  INamedTypeSymbol TypeSymbol           { get; }
        private INamedTypeSymbol UnderlyingTypeSymbol { get; }

        public TypeKind TypeKind      => TypeKind.Struct;
        public string   TypeString    => UnderlyingTypeSymbol.ToDisplayString() + "?";
        public string   FormatterName => TypeSymbol.Name + "FormatterForNullable";

        public string FileName
            => TypeSymbol.ToDisplayString()
                         .Replace("<", "[")
                         .Replace(">", "]")
                         .Replace(",", ".")
             + ".Nullable.Formatter.g.cs";

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

        private void BeginNamespace(StringBuilder sb) {
            if (string.IsNullOrWhiteSpace(TypeSymbol.ContainingNamespace?.Name)) return;
            sb.AppendLine($"namespace {TypeSymbol.ContainingNamespace.ToDisplayString()} {{");
        }

        private void BeginClass(StringBuilder sb) {
            sb.AppendLine($"    public class {FormatterName} : IMessagePackFormatter<{TypeString}> {{");
        }

        private void BuildSerializeMethod(StringBuilder sb) {
            sb.AppendLine($"        public void Serialize(ref MessagePackWriter writer, {TypeString} value, MessagePack.MessagePackSerializerOptions options) {{");
            sb.AppendLine("            if(value.HasValue) {");
            sb.AppendLine("                var resolver = options.Resolver;");
            sb.AppendLine($"                var formatter = resolver.GetFormatterWithVerify<{UnderlyingTypeSymbol.ToDisplayString()}>();");
            sb.AppendLine("                formatter.Serialize(ref writer, value.Value, options);");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine("            writer.WriteNil();");
            sb.AppendLine("        }");
        }

        private void BuildDeserializeMethod(StringBuilder sb) {
            sb.AppendLine($"        public {TypeString} Deserialize(ref MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) {{");
            sb.AppendLine("            if(reader.TryReadNil()) {");
            sb.AppendLine("                return null;");
            sb.AppendLine("            }");
            sb.AppendLine("            var resolver = options.Resolver;");
            sb.AppendLine($"            var formatter = resolver.GetFormatterWithVerify<{UnderlyingTypeSymbol.ToDisplayString()}>();");
            sb.AppendLine("            var value = formatter.Deserialize(ref reader, options);");
            sb.AppendLine($"            return new {TypeString}(value);");
            sb.AppendLine("        }");
        }

        private void BuildUsings(StringBuilder sb) {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Buffers;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using MessagePack;");
            sb.AppendLine("using MessagePack.Formatters;");
            if (!string.IsNullOrWhiteSpace(TypeSymbol.ContainingNamespace?.Name)) {
                sb.AppendLine($"using {TypeSymbol.ContainingNamespace.ToDisplayString()};");
            }

            sb.AppendLine();
        }

        private void EndClass(StringBuilder sb) {
            sb.AppendLine("    }");
        }

        private void EndNamespace(StringBuilder sb) {
            if (string.IsNullOrWhiteSpace(TypeSymbol.ContainingNamespace?.Name)) return;
            sb.AppendLine("}");
        }
    }
}