using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NUnitToMSTestPackage.Utilities
{
    public class OutputWindowPaneHelper : IVsOutputWindowPane
    {
        private readonly Lazy<IVsOutputWindowPane> m_pane;

        public static OutputWindowPaneHelper CreateHosted(string name, Guid guid)
        {
            var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
            return new OutputWindowPaneHelper(outputWindow, name, guid);
        }

        public OutputWindowPaneHelper(IVsOutputWindow outputWindow, string name, Guid guid)
        {
            m_pane = new Lazy<IVsOutputWindowPane>(() =>
            {
                outputWindow.CreatePane(ref guid, name, 1, 1);
                outputWindow.GetPane(ref guid, out var pane);
                return pane;
            });
        }

        public int Hide() => m_pane.Value.Hide();
        public int Activate() => m_pane.Value.Activate();
        public int Clear() => m_pane.Value.Clear();

        public int FlushToTaskList()
        {
            return m_pane.Value.FlushToTaskList();
        }

        int IVsOutputWindowPane.OutputTaskItemString(string pszOutputString, VSTASKPRIORITY nPriority, VSTASKCATEGORY nCategory, string pszSubcategory, int nBitmap, string pszFilename, uint nLineNum, string pszTaskItemText)
        {
            return m_pane.Value.OutputTaskItemString(pszOutputString, nPriority, nCategory, pszSubcategory, nBitmap, pszFilename, nLineNum, pszTaskItemText);
        }

        int IVsOutputWindowPane.OutputTaskItemStringEx(string pszOutputString, VSTASKPRIORITY nPriority, VSTASKCATEGORY nCategory, string pszSubcategory, int nBitmap, string pszFilename, uint nLineNum, string pszTaskItemText, string pszLookupKwd)
        {
            return m_pane.Value.OutputTaskItemStringEx(pszOutputString, nPriority, nCategory, pszSubcategory, nBitmap, pszFilename, nLineNum, pszTaskItemText, pszLookupKwd);
        }

        public int GetName(ref string pbstrPaneName)
        {
            return m_pane.Value.GetName(ref pbstrPaneName);
        }

        public int SetName(string pszPaneName)
        {
            return m_pane.Value.SetName(pszPaneName);
        }

        public int OutputStringThreadSafe(string pszOutputString)
        {
            return OutputString(pszOutputString ?? "");
        }

        public int OutputString(string pszOutputString)
        {
            return OutputString((object)pszOutputString);
        }

        public int OutputString(object obj)
        {
            string str = obj?.ToString() ?? "";
            if (!str.EndsWith("\n", StringComparison.OrdinalIgnoreCase))
                str += "\n";
            return m_pane.Value.OutputStringThreadSafe(str);
        }
    }
}