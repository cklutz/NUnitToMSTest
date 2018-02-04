using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnitToMSTest.Rewriter;

namespace NUnitToMSTestPackage.Refactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(NUnitToMSTestCodeRefactoringProvider)), Shared]
    internal class NUnitToMSTestCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                return;
            if (context.CancellationToken.IsCancellationRequested)
                return;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (!(root.FindNode(context.Span) is UsingDirectiveSyntax node))
                return;
            if (!node.Name.ToFullString().Contains("NUnit"))
                return;

            context.RegisterRefactoring(CodeAction.Create(
                "Convert Tests to MSTest V2", 
                async c =>
                {
                    var tree = await document.GetSyntaxTreeAsync(c);
                    var semanticModel = await document.GetSemanticModelAsync(c);
                    var rw = new NUnitToMSTestRewriter(semanticModel);
                    var result = rw.Visit(tree.GetRoot());

                    return document.WithSyntaxRoot(result);
                }));
        }
    }
}
