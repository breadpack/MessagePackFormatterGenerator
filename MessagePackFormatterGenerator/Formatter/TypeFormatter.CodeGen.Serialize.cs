using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePackFormatterGenerator {
    public partial class TypeFormatter {
        private void BuildSerializeMethod(StringBuilder sb) {
            sb.AppendLine($"        public void Serialize(ref MessagePackWriter writer, {TypeString} value, MessagePack.MessagePackSerializerOptions options) {{");

            if (TypeKind == TypeKind.Class) {
                sb.AppendLine("            if (value == null) { writer.WriteNil(); return; }");
            }

            sb.AppendLine("            var resolver = options.Resolver;");
            sb.AppendLine();

            if (TypeKind == TypeKind.Struct && TypeSymbol.IsBlittableType(out _)) {
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                writer.Write(new ReadOnlySpan<byte>((byte*)&value, sizeof({TypeString})));");
                sb.AppendLine($"            }}");
            }
            else {
                sb.AppendLine("            writer.WriteArrayHeader(" + Members.Length + ");");

                // Private 멤버가 있는 경우, Reflection을 통해 직접 접근
                // struct는 boxing이 발생하기 때문에 private 멤버를 우선적으로 처리
                if (TypeKind == TypeKind.Struct && Members.Any(m => m.DeclaredAccessibility == Accessibility.Private)) {
                    var privateMembers = Members.Where(m => m.DeclaredAccessibility == Accessibility.Private);
                    foreach (var member in privateMembers) {
                        BuildSerializeMember(sb, member);
                    }

                    var publicMembers = Members.Where(m => m.DeclaredAccessibility == Accessibility.Public);
                    foreach (var member in publicMembers) {
                        BuildSerializeMember(sb, member);
                    }
                }
                else {
                    foreach (var member in Members) {
                        BuildSerializeMember(sb, member);
                    }
                }
            }

            sb.AppendLine("        }");
        }

        private void BuildSerializeMember(StringBuilder sb, ISymbol member) {
            var memberType = member switch {
                IFieldSymbol field       => field.Type,
                IPropertySymbol property => property.Type,
                _                        => null
            };

            if (memberType == null) return;

            var memberAccess = member.Name;

            if (IsFixedSizeBuffer(member, out var elementType, out var length)) {
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                    writer.Write(new ReadOnlySpan<byte>(value.{memberAccess}, {length}));");
                sb.AppendLine($"            }}");
            }
            else if (IsSupportedByMessagePack(memberType)) {
                if (member.DeclaredAccessibility == Accessibility.Private) {
                    sb.AppendLine($"            var {memberAccess}Value = typeof({TypeString}).GetField(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);");
                    sb.AppendLine($"            writer.Write(({GetReaderMethodSuffix(memberType)}){memberAccess}Value);");
                }
                else {
                    sb.AppendLine($"            writer.Write(value.{memberAccess});");
                }
            }
            else {
                if (member.DeclaredAccessibility == Accessibility.Private) {
                    sb.AppendLine($"            var {memberAccess}Value = typeof({TypeString}).GetProperty(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(value);");
                    sb.AppendLine($"            MessagePack.MessagePackSerializer.Serialize(ref writer, ({memberType.ToDisplayString()}){memberAccess}Value, options);");
                }
                else {
                    sb.AppendLine($"            MessagePack.MessagePackSerializer.Serialize(ref writer, value.{memberAccess}, options);");
                }
            }
        }
    }
}