using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;

namespace NUnitToMSTest.Rewriter
{
    internal class TreeResult
    {
        public TreeResult(string filePath, Encoding encoding, SyntaxNode treeNode, IEnumerable<Diagnostic> diagnostics, bool changed)
        {
            FilePath = filePath;
            Encoding = encoding;
            TreeNode = treeNode;
            Diagnostics = diagnostics;
            Changed = changed;
        }

        public string FilePath { get; }
        public Encoding Encoding { get; }
        public SyntaxNode TreeNode { get; }
        public IEnumerable<Diagnostic> Diagnostics { get; }
        public bool Changed { get; }

        public void WriteTo(TextWriter tw)
        {
            TreeNode.WriteTo(tw);
        }

        public void WriteToOriginalFile(bool backup = true)
        {
            if (backup)
            {
                IOHelpers.BackupFile(FilePath, 100);
            }

            using (var tw = new StreamWriter(FilePath, false, Encoding))
            {
                WriteTo(tw);
            }
        }
    }
}