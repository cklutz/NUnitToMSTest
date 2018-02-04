using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUnitToMSTest.Rewriter;
using DTEProject = EnvDTE.Project;
using TPL = System.Threading.Tasks;

namespace NUnitToMSTestPackage.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TransformProjectFilesCommand
    {
        // These must match the values in the .vsct file
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("d64eca01-f14a-4283-85c8-43ad91f239fd");

        private readonly Package m_package;
        private readonly ErrorListProvider m_errorListProvider;

        private TransformProjectFilesCommand(Package package, ErrorListProvider errorListProvider)
        {
            m_package = package ?? throw new ArgumentNullException(nameof(package));
            m_errorListProvider = errorListProvider ?? throw new ArgumentNullException(nameof(errorListProvider));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(async (s, e) => await InvokeRefactoring(s, e), menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static TransformProjectFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => m_package;

        public static void Initialize(Package package, ErrorListProvider errorListProvider)
        {
            Instance = new TransformProjectFilesCommand(package, errorListProvider);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private async TPL.Task InvokeRefactoring(object sender, EventArgs e)
        {
            try
            {
                bool hasError = false;
                m_errorListProvider.Tasks.Clear();

                var componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
                var workspace = componentModel.GetService<VisualStudioWorkspace>();
                var solution = workspace.CurrentSolution;
                var newSolution = solution;

                var selectedProject = GetSelectedProject();
                if (selectedProject != null)
                {
                    ((IVsSolution)Package.GetGlobalService(typeof(IVsSolution))).GetProjectOfUniqueName(selectedProject.UniqueName,
                        out var hierarchyItem);

                    string projectName = selectedProject.Name;

                    var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Name == projectName);
                    if (project != null && project.HasDocuments && project.Language == "C#")
                    {
                        var documentIds = project.Documents.Where(document => document.SupportsSemanticModel && document.SupportsSyntaxTree &&
                                                                               document.SourceCodeKind == SourceCodeKind.Regular).Select(d => d.Id);
                        //foreach (var document in project.Documents)
                        foreach (var documentId in documentIds)
                        {
                            var document = project.Documents.First(d => d.Id == documentId);
                            N2MPackage.WriteToOutputPane($"Processing {document.FilePath}");

                            var semanticModel = await document.GetSemanticModelAsync();
                            var tree = await document.GetSyntaxTreeAsync();
                            var rw = new NUnitToMSTestRewriter(semanticModel);
                            var result = rw.Visit(tree.GetRoot());

                            if (rw.Changed)
                            {
                                N2MPackage.WriteToOutputPane($"Saving changes in {document.FilePath}");

                                var newDocument = document.WithSyntaxRoot(result);
                                project = newDocument.Project;
                                newSolution = project.Solution;
                            }

                            foreach (var diag in rw.Diagnostics)
                            {
                                N2MPackage.WriteToOutputPane(diag.ToString());

                                m_errorListProvider.Tasks.Add(ToErrorTask(diag, selectedProject, hierarchyItem));
                                hasError = true;
                            }
                        }

                        if (newSolution != solution)
                        {
                            if (!workspace.TryApplyChanges(newSolution))
                                N2MPackage.ShowErrorBox(ServiceProvider, "Changes not saved.");
                        }
                    }
                }

                if (hasError)
                {
                    m_errorListProvider.Show();
                }
            }
            catch (Exception ex)
            {
                N2MPackage.ShowErrorBox(ServiceProvider, ex.ToString());
            }
        }

        private ErrorTask ToErrorTask(Diagnostic diag, DTEProject project, IVsHierarchy hierarchyItem)
        {
            TaskErrorCategory category;
            switch (diag.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    category = TaskErrorCategory.Message;
                    break;
                case DiagnosticSeverity.Info:
                    category = TaskErrorCategory.Message;
                    break;
                case DiagnosticSeverity.Warning:
                    category = TaskErrorCategory.Warning;
                    break;
                case DiagnosticSeverity.Error:
                    category = TaskErrorCategory.Error;
                    break;
                default:
                    category = TaskErrorCategory.Message;
                    break;
            }

            var error = new ErrorTask
            {
                ErrorCategory = category,
                CanDelete = true,
                Category = TaskCategory.Misc,
                Text = diag.GetMessage(),
                Line = diag.Location.GetLineSpan().StartLinePosition.Line,
                Column = diag.Location.GetLineSpan().StartLinePosition.Character,
                Document = diag.Location.GetLineSpan().Path,
                HierarchyItem = hierarchyItem
            };

            error.Navigate += (sender, e) =>
            {
                //there are two Bugs in the errorListProvider.Navigate method:
                //    Line number needs adjusting
                //    Column is not shown
                error.Line++;
                m_errorListProvider.Navigate(error, new Guid(EnvDTE.Constants.vsViewKindCode));
                error.Line--;
            };

            return error;
        }

        private DTEProject GetSelectedProject()
        {
            Object selectedObject = null;
            IVsMonitorSelection monitorSelection =
                (IVsMonitorSelection)Package.GetGlobalService(
                    typeof(SVsShellMonitorSelection));

            monitorSelection.GetCurrentSelection(out var hierarchyPointer,
                out var projectItemId, out _, out _);

            if (Marshal.GetTypedObjectForIUnknown(hierarchyPointer,
                typeof(IVsHierarchy)) is IVsHierarchy selectedHierarchy)
            {
                ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(
                    projectItemId,
                    (int)__VSHPROPID.VSHPROPID_ExtObject,
                    out selectedObject));
            }

            DTEProject selectedProject = selectedObject as DTEProject;
            return selectedProject;
        }
    }
}
