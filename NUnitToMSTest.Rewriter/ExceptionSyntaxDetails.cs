using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NUnitToMSTest.Rewriter
{
    public class ExceptionSyntaxDetails
    {
        internal bool m_inconclusive;

        public string TypeName { get; set; }
        public MatchType MatchType { get; set; }
        public ArgumentListSyntax MatchArguments { get; set; }
        public string MatchTarget { get; set; }

        internal void SetInconclusive()
        {
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