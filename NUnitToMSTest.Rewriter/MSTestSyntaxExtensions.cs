using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public static class MSTestSyntaxFactory
    {
        /// <summary>
        /// Syntax for <c><![CDATA[Assert.ThrowsException<exceptionType>(expression))]]></c>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exceptionType"></param>
        /// <param name="additionalArguments"></param>
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionSyntax(ExpressionSyntax expression,
            string exceptionType,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
            // Assert.ThrowsException<<ExceptionType>>(() => /* whatever */));

            return SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.GenericName(SyntaxFactory.Identifier("ThrowsException"))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(exceptionType))))))
                .WithArgumentList(
                    BuildArgumentList(expression, additionalArguments));
        }


        /// <summary>
        /// Syntax for <c><![CDATA[Assert.InstanceOfType(Assert.ThrowsException<Exception>(expression, typeof(exceptionType))]]></c>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exceptionType"></param>
        /// <param name="additionalArguments"></param>
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionInstanceOfSyntax(ExpressionSyntax expression,
            string exceptionType, SeparatedSyntaxList<ArgumentSyntax>? additionalArguments = null)
        {
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
                                        SyntaxFactory.IdentifierName(exceptionType)))
                            })));
        }

        private static ArgumentListSyntax BuildArgumentList(ExpressionSyntax expression,
            SeparatedSyntaxList<ArgumentSyntax>? additionalArguments)
        {
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