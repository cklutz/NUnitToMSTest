using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public static class MSTestSyntaxFactory
    {
        public static InvocationExpressionSyntax ThrowsExceptionSyntax(
            ExpressionSyntax expression,
            ExceptionSyntaxDetails details,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));
            if (details.Inconclusive)
                throw new InvalidOperationException();

            if (details.MatchType == MatchType.None)
            {
                return ThrowsExceptionNaked(expression, details, additionalArguments);
            }

            return ThrowsExceptionWithMatch(expression, details, additionalArguments);
        }

        /// <summary>
        /// Syntax for <c><![CDATA[Assert.ThrowsException<exceptionType>(expression))]]></c>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exceptionType"></param>
        /// <param name="additionalArguments"></param>
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionNaked(
            ExpressionSyntax expression,
            ExceptionSyntaxDetails details,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));
            if (details.Inconclusive)
                throw new InvalidOperationException();

            // Assert.ThrowsException<<ExceptionType>>(() => /* whatever */));

            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("ThrowsException"))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(details.TypeName))))))
                .WithArgumentList(
                    BuildArgumentList(expression, additionalArguments));
        }

        public static InvocationExpressionSyntax ThrowsExceptionWithMatch(
            ExpressionSyntax expression, ExceptionSyntaxDetails details,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
            string type = "StringAssert";
            string op;
            var compareArg = details.MatchArguments.Arguments[0];
            switch (details.MatchType)
            {
                case MatchType.Matches:
                    op = "Matches";
                    compareArg = SyntaxFactory.Argument(
                        SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.IdentifierName("System.Text.RegularExpressions.Regex"))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(details.MatchArguments.Arguments[0])))).NormalizeWhitespace();
                    break;
                case MatchType.EqualTo:
                    type = "Assert";
                    op = "AreEqual";
                    break;
                case MatchType.Contains:
                    op = "Contains";
                    break;
                case MatchType.StartsWith:
                    op = "StartsWith";
                    break;
                case MatchType.EndsWith:
                    op = "EndsWith";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var argumentList = new SeparatedSyntaxList<ArgumentSyntax>();
            argumentList = argumentList.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThrowsExceptionNaked(expression, details, additionalArguments),
                        SyntaxFactory.IdentifierName(details.MatchTarget)
                    )));
            argumentList = argumentList.Add(compareArg);

            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(type),
                        SyntaxFactory.IdentifierName(op)))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(argumentList));
        }


        /// <summary>
        /// Syntax for <c><![CDATA[Assert.InstanceOfType(Assert.ThrowsException<Exception>(expression, typeof(exceptionType))]]></c>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exceptionType"></param>
        /// <param name="additionalArguments"></param>
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionInstanceOfSyntax(
            ExpressionSyntax expression,
            ExceptionSyntaxDetails details, SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));
            if (details.Inconclusive)
                throw new InvalidOperationException();

            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.IdentifierName("InstanceOfType")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName("Assert"),
                                                SyntaxFactory.GenericName(
                                                        SyntaxFactory.Identifier("ThrowsException"))
                                                    .WithTypeArgumentList(
                                                        SyntaxFactory.TypeArgumentList(
                                                            SyntaxFactory
                                                                .SingletonSeparatedList<TypeSyntax>(
                                                                    SyntaxFactory.IdentifierName("Exception"))))))
                                        .WithArgumentList(BuildArgumentList(expression, additionalArguments))),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.TypeOfExpression(
                                        SyntaxFactory.IdentifierName(details.TypeName)))
                            })));
        }

        private static ArgumentListSyntax BuildArgumentList(
            ExpressionSyntax expression,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            ArgumentListSyntax argumentList;
            if (additionalArguments.HasValue && additionalArguments.Value.Any())
            {
                var args = new SeparatedSyntaxList<ArgumentSyntax>();
                args = args.Add(SyntaxFactory.Argument(expression));
                args = args.AddRange(additionalArguments.Value);
                argumentList = SyntaxFactory.ArgumentList(args);
            }
            else
            {
                argumentList = SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(expression)));
            }

            return argumentList;
        }
    }
}