using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;

namespace NUnitToMSTest.Tests.Support
{
    public abstract class RefactoringTestBase
    {
        public void TestRefactoring(string inputSource, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(inputSource, expected, afterTest, false);
        }

        public void TestRefactoringWithAsserts(string inputSource, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(inputSource, expected, afterTest, true);
        }

        public void TestRefactoringCore(string inputSource, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest, bool rewriteAsserts)
        {
            try
            {
                var comp = TestSupport.CreateCompilationWithTestReferences(inputSource);
                foreach (var tree in comp.SyntaxTrees)
                {
                    var sm = comp.GetSemanticModel(tree);
                    var root = tree.GetRoot();

                    var rw = new NUnitToMSTestRewriter(sm, rewriteAsserts);
                    var result = rw.Visit(root);

                    afterTest(result, rw);
                }
            }
            catch (AssertFailedException)
            {
                Console.WriteLine(
                    "------------ Original Source -------------------" + Environment.NewLine +
                    inputSource + Environment.NewLine +
                    "------------------------------------------------");
                throw;
            }
        }
    }
}