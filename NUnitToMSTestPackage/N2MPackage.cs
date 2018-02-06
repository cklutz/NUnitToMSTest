using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUnitToMSTestPackage.Commands;
using IServiceProvider = System.IServiceProvider;

namespace NUnitToMSTestPackage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideOptionPage(typeof(OptionPageGrid), "NUnit To MSTest", "NUnit To MSTest", 0, 0, true)]
    public sealed class N2MPackage : Package
    {
        public const string PackageGuidString = "0b2c9d6e-4940-49e0-8c7d-6e75efc2f552";
     
        private ErrorListProvider m_errorListProvider;

        public N2MPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        protected override void Initialize()
        {
            base.Initialize();

            Options.Initialize(this);
            m_errorListProvider = new ErrorListProvider(this);
            TransformProjectFilesCommand.Initialize(this, m_errorListProvider, Options.Instance);
        }

        protected override void Dispose(bool disposing)
        {
            // Currently nothing.
            try
            {

            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public static int ShowInfoBox(IServiceProvider serviceProvider, string message)
        {
            return VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                "NUnit To MSTest",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static int ShowErrorBox(IServiceProvider serviceProvider, string message)
        {
            return VsShellUtilities.ShowMessageBox(
                serviceProvider,
                message,
                "NUnit To MSTest",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static readonly object s_outputLock = new object();
        private static readonly string s_outputWindowGuid = "8E489D7F-EE9B-4233-9708-2430B2CCE9BE";
        private static IVsOutputWindow s_outputWindow;
        private static IVsOutputWindowPane s_outputPane;

        public static void WriteToOutputPane(object obj)
        {
            lock (s_outputLock)
            {
                if (s_outputWindow == null)
                {
                    var guid = new Guid(s_outputWindowGuid);
                    s_outputWindow = (IVsOutputWindow)GetGlobalService(typeof(SVsOutputWindow));
                    s_outputWindow.CreatePane(ref guid, "NUnit To MSTest", 1, 1);
                    s_outputWindow.GetPane(ref guid, out var pane);
                    s_outputPane = pane;
                }

                string str = obj?.ToString() ?? "";
                if (!str.EndsWith("\n", StringComparison.OrdinalIgnoreCase))
                    str += "\n";
                s_outputPane.OutputStringThreadSafe(str);
            }
        }
    }
}
