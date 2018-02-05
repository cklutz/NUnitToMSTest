using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public class ExceptionSyntaxDetails
    {
        private bool m_inconclusive;

        public string TypeName { get; set; }
        public MatchType MatchType { get; set; }
        public ArgumentListSyntax MatchArguments { get; set; }
        public string MatchTarget { get; set; }

        internal ExceptionSyntaxDetails Reset()
        {
            m_inconclusive = false;
            MatchTarget = null;
            MatchType = MatchType.None;
            MatchArguments = null;
            return this;
        }

        internal void SetInconclusive(string context = null,
            [CallerMemberName]string memberName = null, [CallerFilePath]string filePath = null,
            [CallerLineNumber]int lineNumber = 0)
        {
            //if (!m_inconclusive)
            {
                Console.WriteLine("------------------  Inconclusive --------------------------");
                Console.WriteLine($"{filePath}({lineNumber}): In '{memberName}' {context}.");
                Console.WriteLine(Environment.StackTrace);
                Console.WriteLine("-----------------------------------------------------------");
            }
            m_inconclusive = true;
        }

        public bool Inconclusive => m_inconclusive || string.IsNullOrEmpty(TypeName);

        public override string ToString()
        {
            return TypeName + ", " + MatchTarget + "." + MatchType + "(" + MatchArguments + ")";
        }
    }

    public enum MatchType
    {
        None,
        Matches,
        EqualTo,
        Contains,
        StartsWith,
        EndsWith
    }
}