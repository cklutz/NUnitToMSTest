using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NUnitToMSTestPackage.Utilities
{
    public static class MessageBoxExtensions
    {
        private static readonly string s_defaultTitle = typeof(MessageBoxExtensions).Assembly.GetName().Name;

        public static int ShowInfoBox(this IServiceProvider serviceProvider, string message, string title = null)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            title = title ?? s_defaultTitle;

            return VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static int ShowErrorBox(this IServiceProvider serviceProvider, string message, string title = null)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            title = title ?? s_defaultTitle;

            return VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
