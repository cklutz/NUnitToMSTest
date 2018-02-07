using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUnitToMSTestPackage.Commands;
using NUnitToMSTestPackage.Utilities;
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
        private const string OutputWindowPaneGuid = "8E489D7F-EE9B-4233-9708-2430B2CCE9BE";
        
        private ErrorListProvider m_errorListProvider;
        private IVsOutputWindowPane m_outputWindowPane;

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
            var statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            m_errorListProvider = new ErrorListProvider(this);
            m_outputWindowPane = OutputWindowPaneHelper.CreateHosted("NUnit To MSTest", new Guid(OutputWindowPaneGuid));
            TransformProjectFilesCommand.Initialize(this, m_errorListProvider, m_outputWindowPane, statusbar, Options.Instance);
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
    }
}
