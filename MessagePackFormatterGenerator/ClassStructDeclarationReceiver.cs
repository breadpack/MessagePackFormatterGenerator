using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePackFormatterGenerator {
    public class ClassStructDeclarationReceiver : ISyntaxReceiver {
        public List<ClassDeclarationSyntax>  CandidateClasses            { get; } = new();
        public List<StructDeclarationSyntax> CandidateStructs            { get; } = new();
        public List<ClassDeclarationSyntax>  FormatterResolverCandidates { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            switch (syntaxNode) {
                case ClassDeclarationSyntax classDeclarationSyntax
                    when !classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword): {
                    CandidateClasses.Add(classDeclarationSyntax);
                    break;
                }
                case StructDeclarationSyntax structDeclarationSyntax:
                    CandidateStructs.Add(structDeclarationSyntax);
                    break;
            }
        }
    }
}