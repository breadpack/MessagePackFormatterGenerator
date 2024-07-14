using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MessagePackFormatterGenerator {
    public partial class TypeFormatter {
        private static bool IsSupportedByMessagePack(ITypeSymbol type) {
            var specialType = type.OriginalDefinition.SpecialType;
            return specialType is SpecialType.System_Boolean
                               or SpecialType.System_Byte
                               or SpecialType.System_Char
                               or SpecialType.System_Double
                               or SpecialType.System_Single
                               or SpecialType.System_Int32
                               or SpecialType.System_Int64
                               or SpecialType.System_String;
        }

        private static string GetReaderMethodSuffix(ITypeSymbol type) {
            return type.SpecialType switch {
                SpecialType.System_Boolean => "Boolean",
                SpecialType.System_Byte    => "Byte",
                SpecialType.System_Char    => "Char",
                SpecialType.System_Double  => "Double",
                SpecialType.System_Single  => "Single",
                SpecialType.System_Int32   => "Int32",
                SpecialType.System_Int64   => "Int64",
                SpecialType.System_String  => "String",
                _                          => "Object" // 기본 값은 MessagePackSerializer를 통해 처리
            };
        }

        private bool IsFixedSizeBuffer(ISymbol typeSymbol, out ITypeSymbol elementType, out int length) {
            elementType = null;
            length      = 0;

            if (typeSymbol is not IFieldSymbol { IsFixedSizeBuffer: true } fieldSymbol) {
                return false;
            }

            // 소스 코드 위치를 가져옴
            var location = fieldSymbol.Locations.FirstOrDefault();
            if (location == null) {
                return false;
            }

            var syntaxTree = location.SourceTree;
            var text       = syntaxTree.GetText();
            var lineSpan   = location.GetLineSpan();
            var startLine  = lineSpan.StartLinePosition.Line;
            var endLine    = lineSpan.EndLinePosition.Line;

            // 소스 코드에서 해당 필드 정의를 포함하는 모든 라인을 가져옴
            var fieldDefinition = new StringBuilder();
            for (int i = startLine; i <= endLine; i++) {
                fieldDefinition.AppendLine(text.Lines[i].ToString());
            }

            // 고정 크기 배열 패턴을 확인하고 크기를 추출
            var match = System.Text.RegularExpressions.Regex.Match(fieldDefinition.ToString(), @"\[\s*(\d+)\s*\]");
            if (!match.Success) {
                return false;
            }

            // 요소 타입 설정
            elementType = fieldSymbol.Type;

            // 크기 설정
            length = int.Parse(match.Groups[1].Value);
            return true;
        }
    }
}