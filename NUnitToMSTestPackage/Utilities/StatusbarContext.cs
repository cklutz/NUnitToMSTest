using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace NUnitToMSTestPackage.Utilities
{
    public sealed class StatusbarContext : IDisposable
    {
        private bool m_disposed;
        private readonly IVsStatusbar m_statusbar;
        private uint m_statusBarCookie;

        public StatusbarContext(IVsStatusbar statusbar)
        {
            m_statusbar = statusbar ?? throw new ArgumentNullException(nameof(statusbar));
        }

        public void UpdateProgress(string text, int complete, int total)
        {
            m_statusbar.Progress(ref m_statusBarCookie, 1, text, (uint)complete, (uint)total);
        }
        public int Clear() => m_statusbar.Clear();

        public string Text
        {
            get
            {
                m_statusbar.GetText(out var text);
                return text;
            }
            set => m_statusbar.SetText(value);
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_statusbar.Progress(ref m_statusBarCookie, 0, string.Empty, 0, 0);
                m_disposed = true;
            }
        }
    }
}