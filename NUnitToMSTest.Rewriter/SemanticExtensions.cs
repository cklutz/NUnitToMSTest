using System;
using System.Collections.Generic;
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
    }
}
