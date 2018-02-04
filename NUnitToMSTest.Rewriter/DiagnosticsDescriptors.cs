using System;
using Microsoft.CodeAnalysis;

namespace NUnitToMSTest.Rewriter
{
    internal static class DiagnosticsDescriptors
    {
        private const string AttributeRewriteCategory = "AttributeRewrite";

        private static string GetId(int num) => $"NUMS{num:0000}";


        public static readonly DiagnosticDescriptor UnsupportedAttribute = new DiagnosticDescriptor(
            GetId(1),
            nameof(UnsupportedAttribute),
            "Unsupported attribute '[{0}]'. Manual handling required.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor TransformedUnsupported = new DiagnosticDescriptor(
            GetId(2),
            nameof(TransformedUnsupported),
            "Transformed unsupported '[{0}]' to '[{1}]'.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor IgnoredUnsupportedNamedArgument = new DiagnosticDescriptor(
            GetId(3),
            nameof(IgnoredUnsupportedNamedArgument),
            "Ignored unsupported attribute named argument '{0}'.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor IgnoredAllArguments = new DiagnosticDescriptor(
            GetId(4),
            nameof(IgnoredAllArguments),
            "Ignored all attribute arguments on definition '[{0}]'.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor ConvertedArgumentValueToString = new DiagnosticDescriptor(
            GetId(5),
            nameof(ConvertedArgumentValueToString),
            "Convert attribute argument value '{0}' to System.String.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);
    }
}