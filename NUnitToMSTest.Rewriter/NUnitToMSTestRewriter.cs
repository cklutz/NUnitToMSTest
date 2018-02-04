using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public class NUnitToMSTestRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel m_semanticModel;
        private readonly bool m_rewriteAsserts;
        private readonly QualifiedNameSyntax m_namespaceMsTest;
        private readonly QualifiedNameSyntax m_namespaceNUnit;

        private class PerMethodState
        {
            public bool DataRowSeen;
            public ExpressionSyntax Description;

            public void Reset()
            {
                DataRowSeen = false;
                Description = null;
            }
        }

        private readonly PerMethodState m_perMethodState = new PerMethodState();
        private readonly List<Diagnostic> m_diagnostics = new List<Diagnostic>();

        public bool Changed { get; private set; }

        public IEnumerable<Diagnostic> Diagnostics => m_diagnostics;

        public NUnitToMSTestRewriter(SemanticModel semanticModel, bool rewriteAsserts = false)
        {
            m_semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            m_rewriteAsserts = rewriteAsserts;

            m_namespaceMsTest =
                (QualifiedNameSyntax)SyntaxFactory.ParseName("Microsoft.VisualStudio.TestTools.UnitTesting");
            m_namespaceNUnit = (QualifiedNameSyntax)SyntaxFactory.ParseName("NUnit.Framework");
        }

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var existing = m_semanticModel.GetSymbolInfo(node.Name);

            if (m_namespaceNUnit.ToFullString().Equals(existing.Symbol?.ToDisplayString()))
            {
                node = node.WithName(m_namespaceMsTest);
                Changed = true;
            }

            return node;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            try
            {
                if (m_perMethodState.DataRowSeen)
                {
                    node = node.AddAttribute(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("DataTestMethod")));
                    Changed = true;
                }

                if (m_perMethodState.Description != null)
                {
                    var arguments = new SeparatedSyntaxList<AttributeArgumentSyntax>();
                    arguments = arguments.Add(SyntaxFactory.AttributeArgument(m_perMethodState.Description));
                    var description = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Description"));
                    description = description.WithArgumentList(SyntaxFactory.AttributeArgumentList(arguments));

                    node = node.AddAttribute(description);
                    Changed = true;
                }
            }
            finally
            {
                m_perMethodState.Reset();
            }

            return node;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            node = (AttributeSyntax)base.VisitAttribute(node);
            return HandleAttribute(node);
        }

        private SyntaxNode HandleAttribute(AttributeSyntax node)
        {
            var existing = m_semanticModel.GetSymbolInfo(node.Name);

            string existingTypeName = existing.Symbol?.ContainingType?.ToDisplayString();
            var location = node.GetLocation();
            switch (existingTypeName)
            {
                case "NUnit.Framework.SetUpAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("TestInitialize"));
                    break;
                case "NUnit.Framework.TearDownAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("TestCleanup"));
                    break;
                case "NUnit.Framework.OneTimeSetUpAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("ClassInitialize"));
                    break;
                case "NUnit.Framework.OneTimeTearDownAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("ClassCleanup"));
                    break;

                case "NUnit.Framework.PropertyAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("TestProperty"))
                        .ConvertArgumentsToString(m_diagnostics, location);
                    break;

                case "NUnit.Framework.TestFixtureAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("TestClass"))
                        .WithoutArgumentList(m_diagnostics, location);
                    Changed = true;
                    break;
                case "NUnit.Framework.TestCaseAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("DataRow"))
                        .RenameNameEquals("TestName", "DisplayName");
                    m_perMethodState.DataRowSeen = true;
                    Changed = true;
                    break;
                case "NUnit.Framework.TestAttribute":
                    m_perMethodState.Description = node.GetNameEqualsExpression("Description");
                    node = node.WithName(SyntaxFactory.IdentifierName("TestMethod"))
                        .WithoutArgumentList(m_diagnostics, location, "Description");
                    Changed = true;
                    break;
                case "NUnit.Framework.CategoryAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("TestCategory"));
                    Changed = true;
                    break;
                case "NUnit.Framework.ExplicitAttribute":
                    node = TransformExplicitAttribute(node);
                    Changed = true;
                    break;
                case "NUnit.Framework.IgnoreAttribute":
                    node = node.WithName(SyntaxFactory.IdentifierName("Ignore"))
                        .WithoutNameEquals("Until", m_diagnostics, location);
                    Changed = true;
                    break;
                case "NUnit.Framework.DescriptionAttribute"
                    // With MSTest DescriptionAttribute only supported on methods 
                    when node.GetParentKind() == SyntaxKind.MethodDeclaration:
                    node = node.WithName(SyntaxFactory.IdentifierName("Description"));
                    Changed = true;
                    break;
                default:
                    {
                        if (existingTypeName != null && existingTypeName.StartsWith("NUnit."))
                        {
                            // Replace (potential) unqualified name with qualified name.
                            // Otherwise, an attribute whose unqualified name is accidentally the same
                            // as that of some other, unrelated, attribute could semantically change (since we
                            // replace the "using NUnit.Framework" with "using <MSTest>").
                            var fullQualifiedName = SyntaxFactory.ParseName(existingTypeName);
                            m_diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.UnsupportedAttribute, location,
                                node.ToFullString()));
                            node = node.WithName(fullQualifiedName);
                            Changed = true;
                        }

                        break;
                    }
            }

            return node;
        }

        private AttributeSyntax TransformExplicitAttribute(AttributeSyntax node)
        {
            var location = node.GetLocation();
            var original = node.ToFullString();

            // MSTest V2 does not support "[Explicit]".
            // Convert "[Explicit]" to "[Ignore("EXPLICIT")]"
            // Convert "[Explicit("yadayada")]" to "[Ignore("EXPLICIT: yadayada")]"

            string text = "EXPLICIT";
            var description = node.GetPositionExpression(0);
            if (description != null)
            {
                text += ": " + description.GetFirstToken().ValueText;
            }

            var literalExpression = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("\"" + text + "\"", text));

            var arguments = new SeparatedSyntaxList<AttributeArgumentSyntax>();
            arguments = arguments.Add(SyntaxFactory.AttributeArgument(literalExpression));

            node = node.WithName(SyntaxFactory.IdentifierName("Ignore")).WithArgumentList(
                SyntaxFactory.AttributeArgumentList(arguments));

            m_diagnostics.Add(Diagnostic.Create(DiagnosticsDescriptors.TransformedUnsupported, location, original,
                node.ToFullString()));

            return node;
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.VisitInvocationExpression(node);

            if (m_rewriteAsserts)
            {
                var info = m_semanticModel.GetSymbolInfo(node);

                if ("NUnit.Framework.Assert".Equals(info.Symbol?.ContainingType.ToDisplayString()) &&
                    node.Expression is MemberAccessExpressionSyntax ma)
                {
                    if ("That".Equals(ma.Name?.ToString()))
                    {
                        var firstArgument = node.ArgumentList.Arguments.First();
                        var secondArgument = node.ArgumentList.Arguments.Last();

                        if (TryGetExceptionFromThrowsStaticHelper(secondArgument, out var exceptionType) ||
                            TryGetExceptionFromThrowsTypeOf(secondArgument, out exceptionType))
                        {
                            node = MSTestSyntaxFactory.ThrowsExceptionSyntax(firstArgument.Expression, exceptionType)
                                .WithLeadingTrivia(node.GetClosestWhitespaceTrivia(true));
                        }
                        else if (TryGetExceptionFromThrowsInstanceOf(secondArgument, out exceptionType))
                        {
                            node = MSTestSyntaxFactory.ThrowsExceptionInstanceOfSyntax(firstArgument.Expression, exceptionType)
                                .WithLeadingTrivia(node.GetClosestWhitespaceTrivia(true));
                        }
                        else if (HasBooleanResult(firstArgument.Expression, m_semanticModel))
                        {
                            // A simple ==> Assert.That(<boolean expression>); 
                            ma = ma.WithName(SyntaxFactory.IdentifierName("IsTrue"));
                            node = node.WithExpression(ma);
                        }
                    }
                    else if ("Null".Equals(ma.Name?.ToString()))
                    {
                        ma = ma.WithName(SyntaxFactory.IdentifierName("IsNull"));
                        node = node.WithExpression(ma);
                    }
                    else if ("NotNull".Equals(ma.Name?.ToString()))
                    {
                        ma = ma.WithName(SyntaxFactory.IdentifierName("IsNotNull"));
                        node = node.WithExpression(ma);
                    }
                }
            }

            return node;
        }


        private static bool HasBooleanResult(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression != null)
            {
                Console.WriteLine(expression);
                var typeInfo = semanticModel.GetTypeInfo(expression);
                Console.WriteLine(typeInfo.ConvertedType);
                if (typeInfo.ConvertedType?.SpecialType == SpecialType.System_Boolean)
                    return true;
            }

            return false;
        }

        private static bool TryGetExceptionFromThrowsStaticHelper(ArgumentSyntax node, out string name)
        {
            // Handles Assert.That(() => Dummy(), Throws.ArgumentNullException);
            //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            name = null;
            if (node.Expression is MemberAccessExpressionSyntax maes &&
                node.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression &&
                "Throws".Equals(maes.Expression?.ToString()))
            {
                name = maes.Name?.ToString();
                return true;
            }

            return false;
        }

        private static bool TryGetExceptionFromThrowsTypeOf(ArgumentSyntax node, out string name)
        {
            // Handles Assert.That(() => Dummy(), Throws.TypeOf<ArgumentNullException>());
            //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Handles Assert.That(() => Dummy(), Throws.Exception.TypeOf<ArgumentNullException>());
            //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            name = null;

            if (node.Expression is InvocationExpressionSyntax ies &&
                node.Expression.Kind() == SyntaxKind.InvocationExpression &&
                ies.Expression is MemberAccessExpressionSyntax sss &&
                ("Throws".Equals(sss.Expression?.ToString()) ||
                 "Throws.Exception".Equals(sss.Expression?.ToString())) &&
                sss.Name is GenericNameSyntax gns &&
                "TypeOf".Equals(gns.Identifier.ToString()) &&
                gns.TypeArgumentList?.Arguments.Count == 1)
            {
                name = gns.TypeArgumentList.Arguments[0].ToString();
                return true;
            }

            return false;
        }

        private static bool TryGetExceptionFromThrowsInstanceOf(ArgumentSyntax node, out string name)
        {
            // Handles Assert.That(() => Dummy(), Throws.InstanceOf<ArgumentNullException>());
            //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
            // Handles Assert.That(() => Dummy(), Throws.Exception.InstanceOf<ArgumentNullException>());
            //                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

            name = null;

            if (node.Expression is InvocationExpressionSyntax ies &&
                node.Expression.Kind() == SyntaxKind.InvocationExpression &&
                ies.Expression is MemberAccessExpressionSyntax sss &&
                ("Throws".Equals(sss.Expression?.ToString()) ||
                 "Throws.Exception".Equals(sss.Expression?.ToString())) &&
                sss.Name is GenericNameSyntax gns &&
                "InstanceOf".Equals(gns.Identifier.ToString()) &&
                gns.TypeArgumentList?.Arguments.Count == 1)
            {
                name = gns.TypeArgumentList.Arguments[0].ToString();
                return true;
            }

            return false;
        }
    }
}