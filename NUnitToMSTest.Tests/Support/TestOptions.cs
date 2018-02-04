using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NUnitToMSTest.Tests
{
    public static class TestOptions
    {
        // Disable documentation comments by default so that we don't need to
        // document every public member of every test input.
        public static readonly CSharpParseOptions Regular =
            new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None)
                .WithLanguageVersion(LanguageVersion.Latest);

        public static readonly CSharpCompilationOptions ReleaseDll = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
    }
}