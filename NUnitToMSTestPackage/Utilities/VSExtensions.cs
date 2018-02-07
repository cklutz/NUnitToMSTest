using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DTEProject = EnvDTE.Project;

namespace NUnitToMSTestPackage.Utilities
{
    public static class VSExtensions
    {
        public static VisualStudioWorkspace GetRoslynVisualStudioWorkspace(this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            var workspace = componentModel.GetService<VisualStudioWorkspace>();
            return workspace;
        }

        public static IVsHierarchy GetProjectHierarchyItem(this DTEProject project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            var solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
            ErrorHandler.ThrowOnFailure(solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchyItem));
            return hierarchyItem;
        }

        public static DTEProject GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IntPtr ppHier = IntPtr.Zero;
            IntPtr ppSC = IntPtr.Zero;

            try
            {
                var vsMonitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
                vsMonitorSelection.GetCurrentSelection(out ppHier, out var pitemid, out _, out ppSC);

                if (ppHier == IntPtr.Zero)
                {
                    return null;
                }

                // multiple items are selected.
                if (pitemid == (uint)VSConstants.VSITEMID.Selection)
                {
                    return null;
                }

                if (Marshal.GetTypedObjectForIUnknown(ppHier, typeof(IVsHierarchy)) is IVsHierarchy hierarchy)
                {
                    if (hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out var project) >= 0)
                    {
                        return project as DTEProject;
                    }
                }

                return null;
            }
            finally
            {
                if (ppHier != IntPtr.Zero)
                {
                    Marshal.Release(ppHier);
                }
                if (ppSC != IntPtr.Zero)
                {
                    Marshal.Release(ppSC);
                }
            }
        }

        public static ErrorTask AddTask(this ErrorListProvider errorListProvider, Diagnostic diagnostic, DTEProject project)
        {
            if (errorListProvider == null)
                throw new ArgumentNullException(nameof(errorListProvider));
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            TaskErrorCategory category;
            switch (diagnostic.Severity)
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

            var task = new ErrorTask
            {
                ErrorCategory = category,
                CanDelete = true,
                Category = TaskCategory.Misc,
                Text = diagnostic.GetMessage(),
                Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line,
                Column = diagnostic.Location.GetLineSpan().StartLinePosition.Character,
                Document = diagnostic.Location.GetLineSpan().Path,
                HierarchyItem = project.GetProjectHierarchyItem()
            };

            task.Navigate += (sender, e) =>
            {
                //there are two Bugs in the errorListProvider.Navigate method:
                //    Line number needs adjusting
                //    Column is not shown
                task.Line++;
                errorListProvider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindCode));
                task.Line--;
            };

            errorListProvider.Tasks.Add(task);

            return task;
        }
    }
}
