using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public static class SemanticExtensions
    {
        public static bool HasBooleanResult(this SemanticModel semanticModel, ExpressionSyntax expression)
        {
            if (expression != null)
            {
                var typeInfo = semanticModel.GetTypeInfo(expression);
                if (typeInfo.ConvertedType?.SpecialType == SpecialType.System_Boolean)
                    return true;
            }
            return false;
        }

        public static ISymbol FindSymbol<T>(this Compilation compilation, Func<ISymbol, bool> predicate)
            where T : SyntaxNode
        {
            return compilation.SyntaxTrees
                .Select(x => compilation.GetSemanticModel(x))
                .SelectMany(
                    x => x.SyntaxTree
                        .GetRoot()
                        .DescendantNodes()
                        .OfType<T>()
                        .Select(y => x.GetDeclaredSymbol(y)))
                .FirstOrDefault(x => predicate(x));
        }
    }
}
