using System;
using Microsoft.CodeAnalysis;
using NUnitToMSTest.Rewriter;

namespace NUnitToMSTest.Tests.Support
{
    public abstract class RefactoringTestBase
    {
        public void TestRefactoring(string actual, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(actual, expected, afterTest, false);
        }

        public void TestRefactoringWithAsserts(string actual, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(actual, expected, afterTest, true);
        }

        public void TestRefactoringCore(string actual, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest, bool rewriteAsserts)
        {
            var comp = TestSupport.CreateCompilationWithTestReferences(actual);
            foreach (var tree in comp.SyntaxTrees)
            {
                var sm = comp.GetSemanticModel(tree);
                var root = tree.GetRoot();

                var rw = new NUnitToMSTestRewriter(sm, rewriteAsserts);
                var result = rw.Visit(root);

                afterTest(result, rw);
            }
        }
    }
}