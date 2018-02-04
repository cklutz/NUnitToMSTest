using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NUnitToMSTest.Tests
{
    internal class TestSupport
    {
        public static string GetUniqueName()
        {
            return Guid.NewGuid().ToString("D");
        }

        public static CSharpCompilation CreateCompilation(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilation(new[] { Parse(source, options: parseOptions) }, references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithTestReferences(
            string source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilationWithTestReferences(new[] { Parse(source, options: parseOptions) }, references, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilationWithTestReferences(
            IEnumerable<SyntaxTree> source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            var refs = new List<MetadataReference>();
            if (references != null)
            {
                refs.AddRange(references);
            }
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(object))));
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(Enumerable))));
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(NUnit.Framework.TestFixtureAttribute))));

            return CreateCompilation(source, refs, options, assemblyName);
        }

        public static CSharpCompilation CreateCompilation(
            IEnumerable<SyntaxTree> trees,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            if (options == null)
            {
                options = TestOptions.ReleaseDll;
            }

            // Using single-threaded build if debugger attached, to simplify debugging.
            if (Debugger.IsAttached)
            {
                options = options.WithConcurrentBuild(false);
            }

            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(
                assemblyName == "" ? GetUniqueName() : assemblyName,
                trees,
                references,
                options);

            return createCompilationLambda();
        }

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            if ((object)options == null)
            {
                options = TestOptions.Regular;
            }

            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        private static string FileFromType(Type type)
        {
            return type.Assembly.Location;
        }
    }
}