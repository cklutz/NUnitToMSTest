using System;
using System.Collections.Generic;
using System.Text;
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
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionSyntax(ExpressionSyntax expression, string exceptionType)
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
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(expression)))
                );
        }

        /// <summary>
        /// Syntax for <c><![CDATA[Assert.InstanceOfType(Assert.ThrowsException<Exception>(expression, typeof(exceptionType))]]></c>.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="exceptionType"></param>
        /// <returns></returns>
        public static InvocationExpressionSyntax ThrowsExceptionInstanceOfSyntax(ExpressionSyntax expression,
            string exceptionType)
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
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(expression)))
                                        )),
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.TypeOfExpression(
                                        SyntaxFactory.IdentifierName(exceptionType)))
                            })));
        }
    }
}
