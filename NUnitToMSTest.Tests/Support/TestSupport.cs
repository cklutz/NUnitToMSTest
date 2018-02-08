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
            string source0,
            string source1,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "")
        {
            return CreateCompilationWithTestReferences(new[]
            {
                Parse(source0, options: parseOptions),
                Parse(source1, options: parseOptions)
            }, references, options, assemblyName);
        }

        public static List<MetadataReference> GetMetadataReferencesWithTestReferences(
            IEnumerable<MetadataReference> references = null)
        {
            var refs = new List<MetadataReference>();
            if (references != null)
            {
                refs.AddRange(references);
            }
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(object))));
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(Enumerable))));
            refs.Add(MetadataReference.CreateFromFile(FileFromType(typeof(NUnit.Framework.TestFixtureAttribute))));
            return refs;
        }

        public static CSharpCompilation CreateCompilationWithTestReferences(
            IEnumerable<SyntaxTree> source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            string assemblyName = "")
        {
            var refs = GetMetadataReferencesWithTestReferences(references);
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

        
        private static Solution CreateSolution(AdhocWorkspace workspace)
        {
            return workspace.CurrentSolution;
        }

        public static Solution CreateSolutionWithTwoCSharpProjects(AdhocWorkspace workspace, string name1, string source1, string name2, string source2)
        {
            var pm1 = ProjectId.CreateNewId();
            var pm2 = ProjectId.CreateNewId();
            var doc1 = DocumentId.CreateNewId(pm1);
            var doc2 = DocumentId.CreateNewId(pm2);
            var sol = CreateSolution(workspace)
                .AddProject(pm1, name1, name1 + ".dll", LanguageNames.CSharp)
                .WithProjectCompilationOptions(pm1, TestOptions.ReleaseDll)
                .AddProject(pm2, name2, name2 + ".dll", LanguageNames.CSharp)
                .WithProjectCompilationOptions(pm2, TestOptions.ReleaseDll)
                .AddProjectReference(pm2, new ProjectReference(pm1))
                .AddDocument(doc1, name1 + ".cs", SourceText.From(source1))
                .AddDocument(doc2, name2 + ".cs", SourceText.From(source2));

            foreach (var metadataReference in GetMetadataReferencesWithTestReferences())
            {
                sol = sol.AddMetadataReference(pm1, metadataReference);
                sol = sol.AddMetadataReference(pm2, metadataReference);
            }

            return sol;
        }
    }
}