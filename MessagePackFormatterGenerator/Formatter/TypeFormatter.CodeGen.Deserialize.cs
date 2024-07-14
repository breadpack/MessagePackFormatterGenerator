using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePackFormatterGenerator {
    public partial class TypeFormatter {
        private void BuildDeserializeMethod(StringBuilder sb) {
            sb.AppendLine($"        public {TypeString} Deserialize(ref MessagePackReader reader, MessagePack.MessagePackSerializerOptions options) {{");
            if (TypeKind == TypeKind.Class) {
                sb.AppendLine("            if (reader.TryReadNil()) { return default; }");
                sb.AppendLine();
            }

            sb.AppendLine($"            options.Security.DepthStep(ref reader);");
            sb.AppendLine();

            if (TypeKind == TypeKind.Struct && TypeSymbol.IsBlittableType(out _)) {
                sb.AppendLine($"            var result = new {TypeString}();");
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                var bytes = reader.ReadBytes().Value;");
                sb.AppendLine($"                var span = new Span<byte>(&result, sizeof({TypeString}));");
                sb.AppendLine($"                bytes.CopyTo(span);");
                sb.AppendLine($"            }}");
            }
            else {
                sb.AppendLine($"            var count = reader.ReadArrayHeader();");
                sb.AppendLine($"            var result = new {TypeString}();");

                // Private 멤버가 있는 경우, Reflection을 통해 직접 접근
                // struct는 boxing이 발생하기 때문에 private 멤버를 우선적으로 처리
                if (TypeKind == TypeKind.Struct && Members.Any(m => m.DeclaredAccessibility == Accessibility.Private)) {
                    var privateMembers = Members.Where(m => m.DeclaredAccessibility == Accessibility.Private);
                    sb.AppendLine("            var boxedResult = (object)result;");
                    foreach (var member in privateMembers) {
                        BuildDeserializeMemberWithBoxing(sb, member);
                    }

                    sb.AppendLine($"            result = ({TypeString})boxedResult;");
                    var publicMembers = Members.Where(m => m.DeclaredAccessibility == Accessibility.Public);
                    foreach (var member in publicMembers) {
                        BuildDeserializeMember(sb, member);
                    }
                }
                else {
                    foreach (var member in Members) {
                        BuildDeserializeMember(sb, member);
                    }
                }
            }

            sb.AppendLine("            reader.Depth--;");
            sb.AppendLine("            return result;");
            sb.AppendLine("        }");
        }

        private void BuildDeserializeMemberWithBoxing(StringBuilder sb, ISymbol member) {
            var memberType = member switch {
                IFieldSymbol field       => field.Type,
                IPropertySymbol property => property.Type,
                _                        => null
            };

            if (memberType == null) return;

            var memberAccess = member.Name;

            if (IsFixedSizeBuffer(member, out var elementType, out var length)) {
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                var bytes = reader.ReadBytes().Value;");
                sb.AppendLine($"                var span = new Span<byte>(result.{memberAccess}, {length});");
                sb.AppendLine($"                bytes.CopyTo(span);");
                sb.AppendLine($"            }}");
            }
            else if (IsSupportedByMessagePack(memberType)) {
                sb.AppendLine($"            var {memberAccess}Value = reader.Read{GetReaderMethodSuffix(memberType)}();");
                sb.AppendLine($"            typeof({TypeString}).GetField(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxedResult, {memberAccess}Value);");
            }
            else {
                sb.AppendLine($"            var {memberAccess}Value = MessagePack.MessagePackSerializer.Deserialize<{memberType.ToDisplayString()}>(ref reader, options);");
                sb.AppendLine($"            typeof({TypeString}).GetProperty(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boxedResult, {memberAccess}Value);");
            }
        }

        private void BuildDeserializeMember(StringBuilder sb, ISymbol member) {
            var memberType = member switch {
                IFieldSymbol field       => field.Type,
                IPropertySymbol property => property.Type,
                _                        => null
            };

            if (memberType == null) return;

            var memberAccess = member.Name;

            if (IsFixedSizeBuffer(member, out var elementType, out var length)) {
                sb.AppendLine($"            unsafe {{");
                sb.AppendLine($"                var bytes = reader.ReadBytes().Value;");
                sb.AppendLine($"                var span = new Span<byte>(result.{memberAccess}, {length});");
                sb.AppendLine($"                bytes.CopyTo(span);");
                sb.AppendLine($"            }}");
            }
            else if (IsSupportedByMessagePack(memberType)) {
                if (member.DeclaredAccessibility == Accessibility.Private) {
                    sb.AppendLine($"            var {memberAccess}Value = reader.Read{GetReaderMethodSuffix(memberType)}();");
                    sb.AppendLine($"            typeof({TypeString}).GetField(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(result, {memberAccess}Value);");
                }
                else {
                    sb.AppendLine($"            result.{memberAccess} = reader.Read{GetReaderMethodSuffix(memberType)}();");
                }
            }
            else {
                if (member.DeclaredAccessibility == Accessibility.Private) {
                    sb.AppendLine($"            var {memberAccess}Value = MessagePack.MessagePackSerializer.Deserialize<{memberType.ToDisplayString()}>(ref reader, options);");
                    sb.AppendLine($"            typeof({TypeString}).GetProperty(\"{memberAccess}\", BindingFlags.NonPublic | BindingFlags.Instance).SetValue((object)result, {memberAccess}Value);");
                }
                else {
                    sb.AppendLine($"            result.{memberAccess} = MessagePack.MessagePackSerializer.Deserialize<{memberType.ToDisplayString()}>(ref reader, options);");
                }
            }
        }
    }
}