using System;
using System.Collections.Generic;
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
using NUnitToMSTestPackage.Utilities;
using DTEProject = EnvDTE.Project;
using RoslynProject = Microsoft.CodeAnalysis.Project;
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
        private readonly IVsOutputWindowPane m_outputWindowPane;
        private readonly IVsStatusbar m_statusbar;
        private readonly IOptions m_options;

        private TransformProjectFilesCommand(Package package, ErrorListProvider errorListProvider,
            IVsOutputWindowPane outputWindowPane, IVsStatusbar statusbar, IOptions options)
        {
            m_package = package ?? throw new ArgumentNullException(nameof(package));
            m_errorListProvider = errorListProvider ?? throw new ArgumentNullException(nameof(errorListProvider));
            m_outputWindowPane = outputWindowPane ?? throw new ArgumentNullException(nameof(outputWindowPane));
            m_statusbar = statusbar ?? throw new ArgumentNullException(nameof(statusbar));
            m_options = options ?? throw new ArgumentNullException(nameof(options));

            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(async (s, e) => await InvokeRefactoring(s, e), menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static TransformProjectFilesCommand Instance { get; private set; }
        private IServiceProvider ServiceProvider => m_package;

        public static void Initialize(Package package, ErrorListProvider errorListProvider,
            IVsOutputWindowPane outputWindowPane, IVsStatusbar statusbar, IOptions options)
        {
            Instance = new TransformProjectFilesCommand(package, errorListProvider, outputWindowPane, statusbar, options);
        }

        private void OutputMessage(string text, StatusbarContext statusbar, int complete = 0, int total = 0)
        {
            m_outputWindowPane.OutputString(text);
            if (statusbar != null)
            {
                if (total == 0)
                    statusbar.Text = text;
                else
                    statusbar.UpdateProgress(text, complete, total);
            }
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private async TPL.Task InvokeRefactoring(object sender, EventArgs e)
        {
            using (var statusbar = new StatusbarContext(m_statusbar))
            {
                try
                {
                    bool diagnosticsWritten = false;
                    m_errorListProvider.Tasks.Clear();
                    statusbar.Clear();

                    var componentModel = (IComponentModel)ServiceProvider.GetService(typeof(SComponentModel));
                    var workspace = componentModel.GetService<VisualStudioWorkspace>();
                    var solution = workspace.CurrentSolution;
                    var newSolution = solution;

                    var selectedProject = GetSelectedProject();
                    if (selectedProject != null)
                    {
                        var project = workspace.CurrentSolution.Projects.FirstOrDefault(p => p.Name == selectedProject.Name);

                        if (project != null && IsSupportedProject(project))
                        {
                            var hierarchyItem = GetProjectHierarchyItem(selectedProject);
                            var documentIds = GetSupportedDocumentIds(project);
                            int total = CalculateStatusbarTotal(documentIds.Count());
                            int complete = 1;

                            OutputMessage($"Updating project {project.FilePath}.", statusbar);
                            
                            foreach (var documentId in documentIds)
                            {
                                var document = project.Documents.First(d => d.Id == documentId);
                                OutputMessage($"Processing {document.FilePath}", statusbar, complete, total);

                                var semanticModel = await document.GetSemanticModelAsync();
                                var tree = await document.GetSyntaxTreeAsync();
                                var rw = new NUnitToMSTestRewriter(semanticModel, m_options.TransformAsserts);
                                var result = rw.Visit(tree.GetRoot());

                                if (rw.Changed)
                                {
                                    OutputMessage($"Saving changes in {document.FilePath}", statusbar, complete, total);
                                    var newDocument = document.WithSyntaxRoot(result);
                                    project = newDocument.Project;
                                    newSolution = project.Solution;
                                }

                                if (ProcessDiagnostics(rw, selectedProject, hierarchyItem))
                                    diagnosticsWritten = true;

                                complete++;
                            }

                            if (newSolution != solution)
                            {
                                if (m_options.MakeSureProjectFileHasUnitTestType)
                                {
                                    OutputMessage("Ensuring project compatibility", statusbar, ++complete, total);
                                    project = new ProjectUpdater(project).Update();
                                    newSolution = project.Solution;
                                }

                                if (!workspace.TryApplyChanges(newSolution))
                                {
                                    ServiceProvider.ShowErrorBox("Changes not saved.");
                                }

                                // This has to happen after "workspace.TryApplyChanges()", or some internal state
                                // is off, and the apply changes fails. This is because the following modify the
                                // project also/again.
                                AddMSTestPackages(hierarchyItem, selectedProject, statusbar, ref complete, total);
                                RemoveNUnitPackages(hierarchyItem, selectedProject, statusbar, ref complete, total);
                            }
                        }
                    }

                    if (diagnosticsWritten)
                    {
                        m_errorListProvider.Show();
                    }
                }
                catch (Exception ex)
                {
                    ServiceProvider.ShowErrorBox(ex.ToString());
                }
            }
        }

        private int CalculateStatusbarTotal(int documentCount)
        {
            int total = documentCount;
            if (m_options.MakeSureProjectFileHasUnitTestType)
                total++;
            if (m_options.UninstallNUnitPackages)
                total++;
            if (m_options.MSTestPackageVersion != null)
                total++;

            return total;
        }

        private void RemoveNUnitPackages(IVsHierarchy project, DTEProject selectedProject, StatusbarContext statusbar, ref int complete, int total)
        {
            if (m_options.UninstallNUnitPackages)
            {
                OutputMessage("Removing NUnit packages", statusbar, ++complete, total);
                using (var packageInstaller = PackageHandler.CreateHosted(selectedProject, m_outputWindowPane))
                {
                    packageInstaller.ReportWarning = msg =>
                    {
                        var diag = Diagnostic.Create(DiagnosticsDescriptors.CannotRemovePackage, Location.None, msg);
                        m_errorListProvider.Tasks.Add(ToErrorTask(diag, selectedProject, project));
                    };

                    packageInstaller.RemovePackage("NUnit3Adapter");
                    packageInstaller.RemovePackage("NUnit.Console");
                    packageInstaller.RemovePackage("NUnit");
                }
            }
        }

        private void AddMSTestPackages(IVsHierarchy project, DTEProject selectedProject, StatusbarContext statusbar, ref int complete, int total)
        {
            if (!string.IsNullOrWhiteSpace(m_options.MSTestPackageVersion))
            {
                OutputMessage("Adding MSTest packages", statusbar, ++complete, total);
                using (var packageInstaller = PackageHandler.CreateHosted(selectedProject, m_outputWindowPane))
                {
                    packageInstaller.AddPackage("MSTest.TestAdapter", m_options.MSTestPackageVersion);
                    packageInstaller.AddPackage("MSTest.TestFramework", m_options.MSTestPackageVersion);
                }
            }
        }

        private bool ProcessDiagnostics(NUnitToMSTestRewriter rw, DTEProject selectedProject, IVsHierarchy hierarchyItem)
        {
            foreach (var diag in rw.Diagnostics)
            {
                OutputMessage(diag.ToString(), null);
                m_errorListProvider.Tasks.Add(ToErrorTask(diag, selectedProject, hierarchyItem));
                return true;
            }

            return false;
        }

        private static IVsHierarchy GetProjectHierarchyItem(DTEProject selectedProject)
        {
            ((IVsSolution)Package.GetGlobalService(typeof(IVsSolution))).GetProjectOfUniqueName(selectedProject.UniqueName,
                out var hierarchyItem);
            return hierarchyItem;
        }

        private static bool IsSupportedProject(RoslynProject project)
        {
            return project.HasDocuments && project.Language == "C#";
        }

        private static IEnumerable<DocumentId> GetSupportedDocumentIds(RoslynProject project)
        {
            return project.Documents
                .Where(document => document.SupportsSemanticModel &&
                                   document.SupportsSyntaxTree &&
                                   document.SourceCodeKind == SourceCodeKind.Regular)
                .Select(d => d.Id);
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