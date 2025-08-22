using System.Text;

namespace MessagePackFormatterGenerator {
    public partial class TypeFormatter {
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

        private void BeginNamespace(StringBuilder sb) {
            if (string.IsNullOrWhiteSpace(TypeSymbol.ContainingNamespace?.Name)) return;
            sb.AppendLine($"namespace {TypeSymbol.ContainingNamespace.ToDisplayString()} {{");
        }

        private void BeginClass(StringBuilder sb) {
            sb.AppendLine($"    [ExcludeFormatterFromSourceGeneratedResolver]");
            sb.AppendLine($"    public class {FormatterName} : IMessagePackFormatter<{TypeString}> {{");
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