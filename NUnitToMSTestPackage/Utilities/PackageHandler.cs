using System;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio;

namespace NUnitToMSTestPackage.Utilities
{
    internal sealed class PackageHandler : IDisposable
    {
        private readonly EnvDTE.Project m_project;
        private readonly IVsPackageInstallerServices m_packageServices;
        private readonly IVsPackageInstaller m_installer;
        private readonly IVsPackageUninstaller m_uninstaller;
        private readonly IVsPackageInstallerEvents m_events;
        private readonly IVsOutputWindowPane m_outputWindowPane;
        private bool m_disposed;

        public static PackageHandler CreateHosted(EnvDTE.Project project, IVsOutputWindowPane outputWindowPane)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();
            var installer = componentModel.GetService<IVsPackageInstaller>();
            var uninstaller = componentModel.GetService<IVsPackageUninstaller>();
            var events = componentModel.GetService<IVsPackageInstallerEvents>();

            return new PackageHandler(project, installerServices, installer, uninstaller, events, outputWindowPane);
        }

        public PackageHandler(EnvDTE.Project project, IVsPackageInstallerServices packageServices, IVsPackageInstaller installer,
            IVsPackageUninstaller uninstaller, IVsPackageInstallerEvents events)
            : this(project, packageServices, installer, uninstaller, events, null)
        {
            m_project = project;
            m_packageServices = packageServices;
            m_installer = installer;
            m_uninstaller = uninstaller;
            m_events = events;
        }

        public PackageHandler(EnvDTE.Project project, IVsPackageInstallerServices packageServices, IVsPackageInstaller installer,
            IVsPackageUninstaller uninstaller, IVsPackageInstallerEvents events, IVsOutputWindowPane outputWindowPane)
        {
            m_project = project ?? throw new ArgumentNullException(nameof(project));
            m_packageServices = packageServices ?? throw new ArgumentNullException(nameof(packageServices));
            m_installer = installer ?? throw new ArgumentNullException(nameof(installer));
            m_uninstaller = uninstaller ?? throw new ArgumentNullException(nameof(uninstaller));
            m_events = events;
            m_outputWindowPane = outputWindowPane;

            if (m_outputWindowPane != null && m_events != null)
            {
                m_events.PackageInstalling += OnEventsOnPackageInstalling;
                m_events.PackageInstalled += OnEventsOnPackageInstalled;
                m_events.PackageUninstalling += OnEventsOnPackageUninstalling;
                m_events.PackageUninstalled += OnEventsOnPackageUninstalled;
            }
        }

        public Action<string> ReportWarning { get; set; }

        private void OnEventsOnPackageUninstalled(IVsPackageMetadata m)
        {
            m_outputWindowPane.OutputStringThreadSafe($"Uninstalled package {m.Id}, version {m.VersionString}.");
        }

        private void OnEventsOnPackageUninstalling(IVsPackageMetadata m)
        {
            m_outputWindowPane.OutputStringThreadSafe($"Uninstalling package {m.Id}, version {m.VersionString}.");
        }

        private void OnEventsOnPackageInstalled(IVsPackageMetadata m)
        {
            m_outputWindowPane.OutputStringThreadSafe($"Installed package {m.Id}, version {m.VersionString}.");
        }

        private void OnEventsOnPackageInstalling(IVsPackageMetadata m)
        {
            m_outputWindowPane.OutputStringThreadSafe($"Installing package {m.Id}, version {m.VersionString}.");
        }

        public bool RemovePackage(string packageId)
        {
            if (!m_packageServices.IsPackageInstalled(m_project, packageId))
            {
                return false;
            }

            
            var meta = m_packageServices.GetInstalledPackages(m_project).FirstOrDefault(p => p.Id == packageId);
            if (string.IsNullOrEmpty(meta?.InstallPath))
            {
                //
                // UinstallPackage() fails, with the following exception when the respective package is still
                // referenced by the project, but no longer present in the "packages"-folder.
                //
                // So in this case a rebuild of the project _and_ a restore of the (existing/old) NuGet
                // packages is required.
                //
                //  System.ArgumentException: Empty path name is not legal.
                //    at System.IO.FileStream.Init(String path, FileMode mode, FileAccess access, Int32 rights, Boolean useRights, FileShare share, Int32 bufferSize, FileOptions options, SECURITY_ATTRIBUTES secAttrs, String msgPath, Boolean bFromProxy, Boolean useLongPath, Boolean checkHost)
                //    at System.IO.FileStream..ctor(String path, FileMode mode, FileAccess access, FileShare share)
                //    at NuGet.ProjectManagement.MSBuildNuGetProject.<UninstallPackageAsync>d__36.MoveNext()
                //   ...
                //    at NuGet.VisualStudio.VsPackageUninstaller.UninstallPackage(Project project, String packageId, Boolean removeDependencies)
                //    at NUnitToMSTestPackage.Utilities.PackageHandler.RemovePackage(String packageId) in C:\Sources\Stuff\mine\NUnitToMSTest\NUnitToMSTestPackage\Utilities\PackageHandler.cs:line 87
                //    at NUnitToMSTestPackage.Commands.TransformProjectFilesCommand.RemoveNUnitPackages(Project selectedProject, StatusbarContext statusbar, Int32& complete, Int32 total) in C:\Sources\Stuff\mine\NUnitToMSTest\NUnitToMSTestPackage\Commands\TransformProjectFilesCommand.cs:line 186
                //    at NUnitToMSTestPackage.Commands.TransformProjectFilesCommand.<InvokeRefactoring>d__16.MoveNext() in C:\Sources\Stuff\mine\NUnitToMSTest\NUnitToMSTestPackage\Commands\TransformProjectFilesCommand.cs:line 147
                //
                string str = $"Could not remove package {packageId}, because it's InstallPath is not set. " + 
                             "Possibly the package is no longer present in your \"packages\" folder. Packages can " +
                             "only be removed, if they are present there. You could revert what has changed, restore " + 
                             "all existing/old packages and retry the conversion. Or you can manually remove the package " +
                             $"{packageId} from the project {m_project.Name}.";
                m_outputWindowPane?.OutputStringThreadSafe("Warning: " + str);
                ReportWarning?.Invoke(str);
                return false;
            }

            m_uninstaller.UninstallPackage(m_project, packageId, true);
            return true;
        }

        public bool AddPackage(string packageId, string version, string source = null)
        {
            if (m_packageServices.IsPackageInstalled(m_project, packageId))
            {
                return false;
            }

            m_installer.InstallPackage(source ?? "All", m_project, packageId, version, false);
            return true;
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_events != null)
                {
                    m_events.PackageInstalling -= OnEventsOnPackageInstalling;
                    m_events.PackageInstalled -= OnEventsOnPackageInstalled;
                    m_events.PackageUninstalling -= OnEventsOnPackageUninstalling;
                    m_events.PackageUninstalled -= OnEventsOnPackageUninstalled;
                }
                m_disposed = true;
            }
        }
    }
}