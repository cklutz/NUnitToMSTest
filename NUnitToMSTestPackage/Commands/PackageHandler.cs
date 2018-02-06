using System;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace NUnitToMSTestPackage.Commands
{
    internal sealed class PackageHandler : IDisposable
    {
        private readonly EnvDTE.Project m_project;
        private readonly IVsPackageInstallerServices m_packageServices;
        private readonly IVsPackageInstaller m_installer;
        private readonly IVsPackageUninstaller m_uninstaller;
        private readonly IVsPackageInstallerEvents m_events;
        private readonly bool m_cleanup;
        private bool m_disposed;

        public static PackageHandler CreateHosted(EnvDTE.Project project)
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();
            var installer = componentModel.GetService<IVsPackageInstaller>();
            var uninstaller = componentModel.GetService<IVsPackageUninstaller>();
            var events = componentModel.GetService<IVsPackageInstallerEvents>();

            HookEvents(events);
            return new PackageHandler(project, installerServices, installer, uninstaller, events, true);
        }

        public PackageHandler(EnvDTE.Project project, IVsPackageInstallerServices packageServices, IVsPackageInstaller installer,
            IVsPackageUninstaller uninstaller, IVsPackageInstallerEvents events)
            : this(project, packageServices, installer, uninstaller, events, false)
        {
            m_project = project;
            m_packageServices = packageServices;
            m_installer = installer;
            m_uninstaller = uninstaller;
            m_events = events;
        }

        private PackageHandler(EnvDTE.Project project, IVsPackageInstallerServices packageServices, IVsPackageInstaller installer,
            IVsPackageUninstaller uninstaller, IVsPackageInstallerEvents events, bool cleanup)
        {
            m_project = project;
            m_packageServices = packageServices;
            m_installer = installer;
            m_uninstaller = uninstaller;
            m_events = events;
            m_cleanup = cleanup;
        }

        public bool RemovePackage(string packageId)
        {
            if (!m_packageServices.IsPackageInstalled(m_project, packageId))
            {
                return false;
            }

            m_uninstaller.UninstallPackage(m_project, packageId, true);
            return true;
        }

        public bool AddPackage(string packageId, string version)
        {
            if (m_packageServices.IsPackageInstalled(m_project, packageId))
            {
                return false;
            }

            m_installer.InstallPackage("All", m_project, packageId, version, false);
            return true;
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                if (m_cleanup)
                {
                    UnhookEvents(m_events);
                }
                m_disposed = true;
            }
        }

        private static void HookEvents(IVsPackageInstallerEvents events)
        {
            events.PackageInstalling += OnEventsOnPackageInstalling;
            events.PackageInstalled += OnEventsOnPackageInstalled;
            events.PackageUninstalling += OnEventsOnPackageUninstalling;
            events.PackageUninstalled += OnEventsOnPackageUninstalled;
        }

        private static void UnhookEvents(IVsPackageInstallerEvents events)
        {
            events.PackageInstalling -= OnEventsOnPackageInstalling;
            events.PackageInstalled -= OnEventsOnPackageInstalled;
            events.PackageUninstalling -= OnEventsOnPackageUninstalling;
            events.PackageUninstalled -= OnEventsOnPackageUninstalled;
        }

        private static void OnEventsOnPackageUninstalled(IVsPackageMetadata m)
        {
            N2MPackage.WriteToOutputPane($"Uninstalled package {m.Id}, version {m.VersionString}.");
        }

        private static void OnEventsOnPackageUninstalling(IVsPackageMetadata m)
        {
            N2MPackage.WriteToOutputPane($"Uninstalling package {m.Id}, version {m.VersionString}.");
        }

        private static void OnEventsOnPackageInstalled(IVsPackageMetadata m)
        {
            N2MPackage.WriteToOutputPane($"Installed package {m.Id}, version {m.VersionString}.");
        }

        private static void OnEventsOnPackageInstalling(IVsPackageMetadata m)
        {
            N2MPackage.WriteToOutputPane($"Installing package {m.Id}, version {m.VersionString}.");
        }
    }
}