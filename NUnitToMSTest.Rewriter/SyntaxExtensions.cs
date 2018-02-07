using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    internal static class SyntaxExtensions
    {
        private static readonly SyntaxTriviaList s_singleWhitespace = SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "));

        public static ExpressionSyntax CreateInstance(string identifierName, params ArgumentSyntax[] arguments)
        {
            var result = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(identifierName));

            if (arguments != null && arguments.Length > 0)
            {
                var argList = new SeparatedSyntaxList<ArgumentSyntax>();
                argList = argList.AddRange(arguments);
                result = result.WithArgumentList(SyntaxFactory.ArgumentList(argList));
            }

            return result;
        }

        public static bool EqualsString(this ExpressionSyntax expression, string str)
        {
            return expression != null && expression.ToString().Equals(str);
        }

        public static NameSyntax GetName(this SyntaxNode node)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name;
            }
            return null;
        }

        public static NameSyntax GetInvocationName(this InvocationExpressionSyntax node)
        {
            if (node?.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name;
            }

            return null;
        }

        public static ExpressionSyntax GetExpression(this SyntaxNode node)
        {
            if (node is MemberAccessExpressionSyntax memberAccess)
                return memberAccess.Expression;
            if (node is InvocationExpressionSyntax invocation)
                return invocation.Expression;
            if (node is ArgumentSyntax argument)
                return argument.Expression;

            return null;
        }

        public static bool TryGetGenericNameSyntax(this SimpleNameSyntax name, out GenericNameSyntax genericName)
        {
            if (name is GenericNameSyntax gcn)
            {
                genericName = gcn;
                return true;
            }

            genericName = null;
            return false;
        }

        public static int NumberOfArguments(this GenericNameSyntax genericName)
        {
            return genericName.TypeArgumentList?.Arguments.Count ?? 0;
        }

        public static string GetGenericNameIdentifier(this ExpressionSyntax expression, int numberGenericArgumentsConstraint = 0)
        {
            if (expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName)
            {
                if (numberGenericArgumentsConstraint > 0)
                {
                    if (genericName.TypeArgumentList?.Arguments.Count != numberGenericArgumentsConstraint)
                        return null;
                }

                return genericName.Identifier.ToString();
            }

            return null;
        }

        public static SyntaxKind GetParentKind(this AttributeSyntax node)
        {
            if (node.Parent.Kind() == SyntaxKind.AttributeList)
            {
                return node.Parent.Parent.Kind();
            }

            return node.Parent.Kind();
        }

        private static Location GetLocation(this SyntaxNode node, Location trueLocation)
        {
            if (trueLocation != null)
                return trueLocation;
            return node.GetLocation();
        }

        private static AttributeArgumentSyntax FindByNameEquals(AttributeArgumentListSyntax arguments, string nameEquals)
        {
            return arguments?.Arguments.FirstOrDefault(a => nameEquals.Equals(a.NameEquals?.Name?.ToFullString()?.Trim()));
        }

        private static AttributeArgumentSyntax FindByPosition(AttributeArgumentListSyntax arguments, int position)
        {
            if (arguments?.Arguments.Count > position)
            {
                return arguments.Arguments[0];
            }

            return null;
        }

        public static AttributeSyntax RenameNameEquals(this AttributeSyntax node, string nameEquals, string renameTo)
        {
            var candidate = FindByNameEquals(node.ArgumentList, nameEquals);
            if (candidate != null)
            {
                return node.ReplaceNode(candidate, candidate.WithNameEquals(SyntaxFactory.NameEquals(renameTo)));
            }

            return node;
        }

        public static ExpressionSyntax GetNameEqualsExpression(this AttributeSyntax node, string nameEquals)
        {
            var candidate = FindByNameEquals(node.ArgumentList, nameEquals);
            return candidate?.Expression;
        }

        public static ExpressionSyntax GetPositionExpression(this AttributeSyntax node, int position)
        {
            var candidate = FindByPosition(node.ArgumentList, position);
            return candidate?.Expression;
        }

        public static AttributeSyntax WithoutNameEquals(
            this AttributeSyntax node, string nameEquals, List<Diagnostic> diagnostics = null,
            Location location = null)
        {
            var candidate = FindByNameEquals(node.ArgumentList, nameEquals);

            if (candidate != null)
            {
                diagnostics?.Add(Diagnostic.Create(DiagnosticsDescriptors.IgnoredUnsupportedNamedArgument,
                    node.GetLocation(location), nameEquals));

                return node.WithArgumentList(node.ArgumentList.RemoveNode(candidate, SyntaxRemoveOptions.KeepDirectives));
            }

            return node;
        }

        public static AttributeSyntax WithoutArgumentList(
            this AttributeSyntax node, List<Diagnostic> diagnostics = null, Location location = null,
            params string[] ignore)
        {
            if (node.ArgumentList?.Arguments.Count > 0)
            {
                int count = node.ArgumentList.Arguments.Count(a => ignore.All(i => i != a.NameEquals?.Name?.ToFullString()?.Trim()));
                if (count > 0)
                {
                    diagnostics?.Add(Diagnostic.Create(DiagnosticsDescriptors.IgnoredAllArguments,
                        node.GetLocation(location), node.ToFullString()));
                }

                node = node.WithArgumentList(null);
            }

            return node;
        }

        public static AttributeSyntax ConvertArgumentsToString(this AttributeSyntax node, List<Diagnostic> diagnostics = null, Location location = null)
        {
            var newList = new SeparatedSyntaxList<AttributeArgumentSyntax>();
            foreach (var entry in node.ArgumentList.Arguments)
            {
                if (entry.Expression.Kind() != SyntaxKind.StringLiteralExpression)
                {
                    var existingToken = entry.Expression.GetFirstToken();
                    diagnostics?.Add(Diagnostic.Create(DiagnosticsDescriptors.ConvertedArgumentValueToString, node.GetLocation(location), existingToken.ValueText));

                    var token = SyntaxFactory.Token(s_singleWhitespace, SyntaxKind.StringLiteralToken,
                        "\"" + existingToken.Text + "\"", existingToken.ValueText, SyntaxTriviaList.Empty);
                    var newEntry = entry.WithExpression(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression).WithToken(token));
                    newList = newList.Add(newEntry);
                }
                else
                {
                    newList = newList.Add(entry);
                }
            }

            return node.WithArgumentList(SyntaxFactory.AttributeArgumentList(newList));
        }

        public static MethodDeclarationSyntax AddAttribute(this MethodDeclarationSyntax node, AttributeSyntax attr)
        {
            var existing = node.AttributeLists;
            var last = existing.Last();

            var list = new SeparatedSyntaxList<AttributeSyntax>();
            list = list.Add(attr);

            var newList = SyntaxFactory.AttributeList(list)
                .WithLeadingTrivia(last.GetClosestWhitespaceTrivia(true))
                .WithTrailingTrivia(last.GetClosestWhitespaceTrivia(false));

            existing = existing.Add(newList);

            node = node.WithAttributeLists(existing);
            return node;
        }

        public static SyntaxTriviaList GetClosestWhitespaceTrivia(this SyntaxNode node, bool leading)
        {
            var list = leading ? node.GetLeadingTrivia() : node.GetTrailingTrivia();
            if (list.Count > 0)
            {
                var lastTrivia = list.Last();
                switch ((SyntaxKind)lastTrivia.RawKind)
                {
                    case SyntaxKind.WhitespaceTrivia:
                        return SyntaxTriviaList.Create(lastTrivia);
                    case SyntaxKind.EndOfLineTrivia:
                        return SyntaxTriviaList.Create(lastTrivia);
                }
            }

            return SyntaxTriviaList.Empty;
        }

        public static bool TypeSymbolMatchesType(this SemanticModel semanticModel, ITypeSymbol typeSymbol, Type type)
        {
            return GetTypeSymbolForType(type, semanticModel).Equals(typeSymbol);
        }

        private static INamedTypeSymbol GetTypeSymbolForType(Type type, SemanticModel semanticModel)
        {
            if (!type.IsConstructedGenericType)
            {
                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName);
            }

            // get all typeInfo's for the Type arguments 
            var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => GetTypeSymbolForType(a, semanticModel));

            var openType = type.GetGenericTypeDefinition();
            var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);
            return typeSymbol.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
        }

        public static string GetLiteralString(this ExpressionSyntax arg)
        {
            if (arg is LiteralExpressionSyntax)
            {
                // Remove quotes.
                string str = arg.ToString().Trim('"');
                if (SyntaxFacts.IsValidIdentifier(str))
                {
                    return str;
                }
            }

            // We can handle one invocation expression and that is "nameof(...)".
            if (arg is InvocationExpressionSyntax invocation &&
                invocation.Expression.EqualsString("nameof"))
            {
                var n = SyntaxFactory.ParseName(invocation.ArgumentList.Arguments[0].ToString());
                if (n is QualifiedNameSyntax qn)
                {
                    return qn.Right.ToString();
                }
                if (n is IdentifierNameSyntax iname)
                {
                    return iname.Identifier.ToString();
                }
            }

            return null;
        }

        public static ArgumentListSyntax TransformParentInvocationArguments(
            this MemberAccessExpressionSyntax memberAccess,
            ExceptionSyntaxDetails details, int numArgumentsRequired,
            Func<ArgumentSyntax, int, ArgumentSyntax> transform
            )
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            if (memberAccess?.Parent is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList?.Arguments.Count == numArgumentsRequired)
            {
                var result = new SeparatedSyntaxList<ArgumentSyntax>();
                for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
                {
                    var transformed = transform(invocation.ArgumentList.Arguments[i], i);
                    if (transformed == null)
                    {
                        details.SetInconclusive(invocation.ArgumentList.Arguments[i]?.ToString());
                        return null;
                    }
                    result = result.Add(transformed);
                }
                return SyntaxFactory.ArgumentList(result);
            }

            details.SetInconclusive(memberAccess?.ToString());
            return null;
        }

        public static ArgumentListSyntax GetParentInvocationArguments(
            this MemberAccessExpressionSyntax memberAccess,
            ExceptionSyntaxDetails details, int numArgumentsRequired,
            Func<ArgumentSyntax, int, bool> check = null)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));

            if (memberAccess?.Parent is InvocationExpressionSyntax invocation &&
                invocation.ArgumentList?.Arguments.Count == numArgumentsRequired)
            {
                for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
                {
                    if (check != null && !check(invocation.ArgumentList.Arguments[i], i))
                    {
                        details.SetInconclusive(invocation.ArgumentList.Arguments[i]?.ToString());
                        return null;
                    }
                }

                return invocation.ArgumentList;
            }

            details.SetInconclusive(memberAccess?.ToString());
            return null;
        }
    }
}