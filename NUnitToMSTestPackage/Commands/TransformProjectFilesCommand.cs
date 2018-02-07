using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        private TransformProjectFilesCommand(
            Package package, ErrorListProvider errorListProvider,
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

        public static void Initialize(
            Package package, ErrorListProvider errorListProvider,
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

                    var workspace = ServiceProvider.GetRoslynVisualStudioWorkspace();
                    var solution = workspace.CurrentSolution;
                    var newSolution = solution;

                    var selectedProject = VSExtensions.GetSelectedProject();
                    if (selectedProject != null)
                    {
                        var project = solution.Projects.FirstOrDefault(p => p.Name == selectedProject.Name);

                        if (project != null && IsSupportedProject(project))
                        {
                            //var hierarchyItem = GetProjectHierarchyItem(selectedProject);
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

                                if (ProcessDiagnostics(rw, selectedProject))
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
                                AddMSTestPackages(selectedProject, statusbar, ref complete, total);
                                RemoveNUnitPackages(selectedProject, statusbar, ref complete, total);
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
                total += m_options.NUnitPackages?.Length ?? 0;
            if (m_options.InstallMSTestPackages)
                total += m_options.MSTestPackages?.Length ?? 0;

            return total;
        }

        private void RemoveNUnitPackages(DTEProject selectedProject, StatusbarContext statusbar, ref int complete, int total)
        {
            if (m_options.UninstallNUnitPackages && m_options.NUnitPackages?.Length > 0)
            {
                OutputMessage("Removing NUnit packages", statusbar, ++complete, total);
                using (var packageInstaller = PackageHandler.CreateHosted(selectedProject, m_outputWindowPane))
                {
                    packageInstaller.ReportWarning = msg =>
                    {
                        var diag = Diagnostic.Create(DiagnosticsDescriptors.CannotRemovePackage, Location.None, msg);
                        m_errorListProvider.AddTask(diag, selectedProject);
                    };

                    foreach (var package in m_options.NUnitPackages)
                    {
                        packageInstaller.RemovePackage(package.Trim());
                    }
                }
            }
        }

        private void AddMSTestPackages(DTEProject selectedProject, StatusbarContext statusbar, ref int complete, int total)
        {
            if (m_options.InstallMSTestPackages && m_options.MSTestPackages?.Length > 0)
            {
                OutputMessage("Adding MSTest packages", statusbar, ++complete, total);
                using (var packageInstaller = PackageHandler.CreateHosted(selectedProject, m_outputWindowPane))
                {
                    foreach (var package in m_options.MSTestPackages)
                    {
                        var spec = package.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                        if (spec.Count > 1)
                        {
                            packageInstaller.AddPackage(spec[0], spec[1]);
                        }
                        else
                        {
                            packageInstaller.AddPackage(spec[0]);
                        }
                    }
                }
            }
        }

        private bool ProcessDiagnostics(NUnitToMSTestRewriter rw, DTEProject selectedProject)
        {
            foreach (var diag in rw.Diagnostics)
            {
                OutputMessage(diag.ToString(), null);
                m_errorListProvider.AddTask(diag, selectedProject);
                return true;
            }

            return false;
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
    }
}