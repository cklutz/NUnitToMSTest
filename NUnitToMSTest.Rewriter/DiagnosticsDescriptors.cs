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

        public static readonly DiagnosticDescriptor IncompatibleClassInitiazeMethod = new DiagnosticDescriptor(
            GetId(6),
            nameof(IncompatibleClassInitiazeMethod),
            "Method '{0}' has wrong signature for use as {1}. The method must be static, public, does not return a value and have a parameter of type TestContext. "+
            "Additionally, if you are using async-await in method then return-type must be Task.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor IncompatibleClassCleanupMethod = new DiagnosticDescriptor(
            GetId(7),
            nameof(IncompatibleClassCleanupMethod),
            "Method '{0}' has wrong signature for use as {1}. The method must be static, public, does not return a value and have no parameters. "+
            "Additionally, if you are using async-await in method then return-type must be Task.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor IncompatibleTestInitiazeMethod = new DiagnosticDescriptor(
            GetId(8),
            nameof(IncompatibleTestInitiazeMethod),
            "Method '{0}' has wrong signature for use as {1}. The method must be non-static, public, does not return a value and should not take any parameter. " +
            "Additionally, if you are using async-await in method then return-type must be Task.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor IncompatibleTestCleanupMethod = new DiagnosticDescriptor(
            GetId(9),
            nameof(IncompatibleTestCleanupMethod),
            "Method '{0}' has wrong signature for use as {1}. The method must be non-static, public, does not return a value and should not take any parameter. " +
            "Additionally, if you are using async-await in method then return-type must be Task.",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor UnsupportedAttributeUsage = new DiagnosticDescriptor(
            GetId(10),
            nameof(UnsupportedAttributeUsage),
            "The usage of the attribute '[{0}]' is not supported. Manual handling required. {1}",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);

        public static readonly DiagnosticDescriptor CannotRemovePackage = new DiagnosticDescriptor(
            GetId(11),
            nameof(CannotRemovePackage),
            "{0}",
            AttributeRewriteCategory,
            DiagnosticSeverity.Warning,
            true);
    }
}