using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnitToMSTest.Rewriter;

namespace NUnitToMSTest.Tests.Support
{
    public abstract class RefactoringTestBase
    {
        public void TestRefactoring(string inputSource, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(inputSource, expected, null, afterTest, false);
        }

        public void TestRefactoringWithAsserts(string inputSource, string expected, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(inputSource, expected, null, afterTest, true);
        }

        public void TestRefactoringWithAsserts(string inputSource, string expected, string auxiliary, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest)
        {
            TestRefactoringCore(inputSource, expected, auxiliary, afterTest, true);
        }


        public void TestRefactoringCore(string inputSource, string expected, string auxiliary, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest, bool rewriteAsserts)
        {
            try
            {
                Compilation comp = auxiliary != null ?
                    TestSupport.CreateCompilationWithTestReferences(inputSource, auxiliary) :
                    TestSupport.CreateCompilationWithTestReferences(inputSource);

                // TODO: Some unit test sources need to fixed first.
                //var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
                //if (errors.Any())
                //{
                //    Assert.Fail("Test cannot run, project: {0}", string.Join(Environment.NewLine, errors));
                //}

                bool seen = false;
                foreach (var tree in comp.SyntaxTrees)
                {
                    seen = true;
                    var sm = comp.GetSemanticModel(tree);
                    var root = tree.GetRoot();

                    var rw = new NUnitToMSTestRewriter(sm, rewriteAsserts);
                    var result = rw.Visit(root);

                    afterTest(result, rw);
                }

                if (!seen)
                {
                    Assert.Fail("Test produced no SyntaxTrees to compare.");
                }
            }
            catch (AssertFailedException)
            {
                Console.WriteLine(
                    "------------ Original Source -------------------" + Environment.NewLine +
                    inputSource + Environment.NewLine +
                    "------------------------------------------------");

                if (auxiliary != null)
                {
                    Console.WriteLine(
                        "------------ Auxiliary Source ------------------" + Environment.NewLine +
                        auxiliary + Environment.NewLine +
                        "------------------------------------------------");
                }

                throw;
            }
        }

        public void TestRefactoringMultipCompilations(string inputSource, string expected, string auxiliary, Action<SyntaxNode, NUnitToMSTestRewriter> afterTest, bool rewriteAsserts)
        {

            try
            {
                var workspace = new AdhocWorkspace();
                var solution = TestSupport.CreateSolutionWithTwoCSharpProjects(workspace, nameof(auxiliary), auxiliary, nameof(inputSource), inputSource);

                bool seen = false;
                foreach (var project in solution.Projects)
                {
                    var comp = project.GetCompilationAsync().Result;
                    var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
                    if (errors.Any())
                    {
                        Assert.Fail("Test cannot run, project {0}: {1}", project.Name,
                            string.Join(Environment.NewLine, errors));
                    }

                    if (project.Name == nameof(inputSource))
                    {
                        foreach (var tree in comp.SyntaxTrees)
                        {
                            seen = true;
                            var sm = comp.GetSemanticModel(tree);
                            var root = tree.GetRoot();

                            var rw = new NUnitToMSTestRewriter(sm, rewriteAsserts);
                            var result = rw.Visit(root);

                            afterTest(result, rw);
                        }
                    }
                }

                if (!seen)
                {
                    Assert.Fail("Test produced no SyntaxTrees to compare.");
                }
            }
            catch (AssertFailedException)
            {
                Console.WriteLine(
                    "------------ Original Source -------------------" + Environment.NewLine +
                    inputSource + Environment.NewLine +
                    "------------------------------------------------");

                if (auxiliary != null)
                {
                    Console.WriteLine(
                        "------------ Auxiliary Source ------------------" + Environment.NewLine +
                        auxiliary + Environment.NewLine +
                        "------------------------------------------------");
                }

                throw;
            }
        }
    }
}