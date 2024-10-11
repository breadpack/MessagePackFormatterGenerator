using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MessagePackFormatterGenerator {
    public static class NamedTypeSymbolExtensions {
        public static INamedTypeSymbol GetUnderlyingOrSelfType(this INamedTypeSymbol symbol) {
            if (symbol.NullableAnnotation == NullableAnnotation.Annotated) {
                return symbol.OriginalDefinition;
            }
            // Nullable 타입인지 확인하고 underlying 타입을 반환, 아니면 원래 타입을 반환
            if (symbol.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                return symbol;
            
            var underlyingType = symbol.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
            return underlyingType ?? symbol;
        }
        
        public static bool HasAttribute(this INamedTypeSymbol symbol, INamedTypeSymbol attributeSymbol, bool inherit = false) {
            if (symbol == null || attributeSymbol == null) {
                return false;
            }

            if (symbol.GetAttributes()
                      .Where(attr => attr.AttributeClass != null)
                      .Any(attr => attr.AttributeClass.ToDisplayString() == attributeSymbol.ToDisplayString())
               ) {
                return true;
            }

            return inherit && symbol.BaseType?.HasAttribute(attributeSymbol, true) == true;
        }

        public static bool IsBlittableType(this ITypeSymbol type, out string reason) {
            var visited = new HashSet<string>();
            return IsBlittableType(type, out reason, visited);
        }

        private static bool IsBlittableType(this ITypeSymbol type, out string reason, HashSet<string> visited) {
            reason = string.Empty;

            if (!visited.Add(type.ToDisplayString()))
                return true;

            switch (type.SpecialType) {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Char:
                    return true;
            }

            if (type.TypeKind == TypeKind.Enum)
                return true;

            if (type.TypeKind == TypeKind.Struct) {
                foreach (var member in type.GetMembers().OfType<IFieldSymbol>()) {
                    if (member.IsFixedSizeBuffer) {
                        continue;
                    }

                    if (member.Type is IPointerTypeSymbol) {
                        reason = $"Field '{member.Name}' is a pointer";
                        return false;
                    }

                    if (member.Type.IsBlittableType(out reason, visited))
                        continue;

                    reason = $"Field '{member.Name}' is not blittable. - {reason}";
                    return false;
                }

                return true;
            }

            reason = $"Type '{type.Name}' is not blittable";
            return false;
        }

        /// <summary>
        /// 멤버 필드 타입을 탐색하여 관련된 모든 타입을 반환합니다.
        /// Array, List, Dictionary 등의 제네릭 타입의 경우, 제네릭 타입의 타입 인자도 반환합니다.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="namedTypeSymbols"></param>
        /// <param name="recursive">재귀적 탐색 여부</param>
        /// <returns></returns>
        public static IEnumerable<INamedTypeSymbol> GetRelatedTypes(this INamedTypeSymbol symbol, bool recursive = false) {
            var collectedTypes = new HashSet<INamedTypeSymbol>();
            return symbol.GetRelatedTypes(collectedTypes, recursive);
        }
        
        public static bool IsMatchWith(this INamedTypeSymbol typeSymbol, INamedTypeSymbol predefinedGenericType, SymbolEqualityComparer comparer = null) {
            if (!predefinedGenericType.IsGenericType)
                return typeSymbol.Equals(predefinedGenericType, comparer ?? SymbolEqualityComparer.Default);

            if (typeSymbol.OriginalDefinition.Equals(predefinedGenericType.OriginalDefinition, comparer ?? SymbolEqualityComparer.Default)) {
                return true;
            }

            return typeSymbol.AllInterfaces.Any(i => i.OriginalDefinition.Equals(predefinedGenericType, SymbolEqualityComparer.Default));
        }

        public static IEnumerable<INamedTypeSymbol> GetRelatedTypes(this INamedTypeSymbol typeSymbol, HashSet<INamedTypeSymbol> collectedTypes, bool recursive) {
            if (!collectedTypes.Add(typeSymbol)) {
                return Enumerable.Empty<INamedTypeSymbol>();
            }

            return typeSymbol.GetMembers()
                             .Where(m => m switch {
                                 IFieldSymbol field => !field.IsConst
                                                    && !field.IsStatic
                                                    && !field.IsImplicitlyDeclared,
                                 IPropertySymbol property => property.SetMethod != null,
                                 _                        => false,
                             })
                             .Select(m => m switch {
                                 IFieldSymbol field       => field.Type,
                                 IPropertySymbol property => property.Type,
                                 _                        => null
                             })
                             .SelectMany(GetAllTypes)
                             .Where(t =>
                                        t.TypeKind is TypeKind.Array
                                                   or TypeKind.Class
                                                   or TypeKind.Struct
                                                   or TypeKind.Pointer)
                             .Distinct()
                             .SelectMany(type => type.GetRelatedTypes(collectedTypes, recursive))
                             .Concat(new[] { typeSymbol });
        }

        private static IEnumerable<INamedTypeSymbol> GetAllTypes(ITypeSymbol typeSymbol) {
            if (typeSymbol == null) {
                yield break;
            }

            while (true) {
                // Direct type
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol) {
                    yield return namedTypeSymbol;

                    // Handle generic types like List<T>, Dictionary<K, V>
                    if (namedTypeSymbol.IsGenericType) {
                        foreach (var typeArgument in namedTypeSymbol.TypeArguments) {
                            if (typeArgument is INamedTypeSymbol nestedTypeArgument) {
                                yield return nestedTypeArgument;
                                foreach (var nested in GetAllTypes(nestedTypeArgument)) {
                                    yield return nested;
                                }
                            }
                        }
                    }
                }

                // Handle array types
                if (typeSymbol is IArrayTypeSymbol { ElementType: INamedTypeSymbol elementType }) {
                    yield return elementType;
                    foreach (var nested in GetAllTypes(elementType)) {
                        yield return nested;
                    }
                }

                // Handle other types that may contain nested types
                if (typeSymbol is IPointerTypeSymbol { PointedAtType: INamedTypeSymbol pointedAtType }) {
                    yield return pointedAtType;
                    typeSymbol = pointedAtType;
                    continue;
                }

                break;
            }
        }
    }
}