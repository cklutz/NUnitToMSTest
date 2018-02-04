using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using NUnitToMSTest.Rewriter;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NUnitToMSTestRefactoring")]


namespace NUnitToMSTest
{
    internal class Program
    {
        private static bool s_outputToConsole;
        private static bool s_backupOriginal;
        private static bool s_warningAsErrors;
        private static bool s_verbose;

        private static async Task<int> Main(string[] args)
        {
            try
            {
                //Environment.SetEnvironmentVariable("VSINSTALLDIR", @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\");
                //Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");

                var options = new Dictionary<string, string>();
                if (!ParseArgs(ref args, options) || args.Length < 1)
                {
                    Console.Error.WriteLine(@"
Usage: {0} [OPTIONS] <csproj-file>

Convert (.cs) source files in the specified project from NUnit (3.x)
to MSTest V2.
                        
Options:
-p:KEY=VALUE Pass option to MSBuild.
-console     Output changes to console instead of modifying source files.
-backup      Create backup files of changed sources.
-warnaserror Treat warnings as errors.
-verbose     Issue verbose messages.
",
                        typeof(Program).Assembly.GetName().Name);
                    return 99;
                }

                string projectFilePath = args[0];
                Verbose($"Loading project '{projectFilePath}'.");

                var workspace = MSBuildWorkspace.Create(options);
                //workspace.LoadMetadataForReferencedProjects = true;

                var project = await workspace.OpenProjectAsync(projectFilePath);
                if (DumpDiagnostics(workspace.Diagnostics))
                    return 1;
                
                Verbose($"Project.CompilationOptions.Platform: {project.CompilationOptions.Platform}");
                Verbose($"Project.CompilationOptions.Language: {project.CompilationOptions.Language}");
                Verbose($"Project.CompilationOptions.OptimizationLevel: {project.CompilationOptions.OptimizationLevel}");
                Verbose($"Project.CompilationOptions.WarningLevel: {project.CompilationOptions.WarningLevel}");
                Verbose($"Project.CompilationOptions.OutputKind: {project.CompilationOptions.OutputKind}");

                //foreach (var r in project.MetadataReferences)
                //{
                //    Verbose($"Project.MetadataReference.Display: {r.Display}");
                //}

                //foreach (var r in project.Documents)
                //{
                //    Verbose($"Project.Documents.FilePath: {r.FilePath}");
                //}

                Verbose("Loaded, hit any key to continue...");
                Console.ReadKey();

                

                //var compilation = await project.GetCompilationAsync();
                //if (DumpDiagnostics(compilation.GetDiagnostics()))
                //    return 1;

                var result = await RewriteSourcesAsync(project.Documents);
                if (DumpDiagnostics(result))
                    return 1;

                UpdateSources(result);
            }
            catch (Exception ex)
            {
                Error(ex.ToString());
                return ex.HResult;
            }

            return 0;
        }

        private static async Task<List<TreeResult>> RewriteSourcesAsync(IEnumerable<Document> documents)
        {
            var results = new List<TreeResult>();

            foreach (var document in documents)
            {
                var semanticModel = await document.GetSemanticModelAsync();
                var tree = await document.GetSyntaxTreeAsync();
                var rw = new NUnitToMSTestRewriter(semanticModel);
                var result = rw.Visit(tree.GetRoot());
                var treeResult = new TreeResult(tree.FilePath, tree.Encoding, result, rw.Diagnostics, rw.Changed);
                results.Add(treeResult);
            }
            
            return results;
        }

        private static List<TreeResult> RewriteSources(Compilation compilation)
        {
            var resultsLock = new object();
            var results = new List<TreeResult>();

            var p = Parallel.ForEach(
                compilation.SyntaxTrees,
                () => new List<TreeResult>(),
                (tree, state, local) =>
                {
                    if (!state.ShouldExitCurrentIteration)
                    {
                        try
                        {
                            var semanticModel = compilation.GetSemanticModel(tree);
                            var rw = new NUnitToMSTestRewriter(semanticModel);
                            var result = rw.Visit(tree.GetRoot());
                            var treeResult = new TreeResult(tree.FilePath, tree.Encoding, result, rw.Diagnostics, rw.Changed);
                            local.Add(treeResult);
                        }
                        catch (Exception ex)
                        {
                            Error(ex.ToString());
                            state.Stop();
                        }
                    }

                    return local;
                },
                local =>
                {
                    lock (resultsLock)
                    {
                        results.AddRange(local);
                    }
                });

            if (DumpDiagnostics(results) || !p.IsCompleted)
            {
                return null;
            }

            return results;
        }

        private static void UpdateSources(List<TreeResult> results)
        {
            bool any = false;
            foreach (var result in results.Where(r => r.Changed))
            {
                any = true;
                if (s_outputToConsole)
                {
                    result.WriteTo(Console.Out);
                }
                else
                {
                    result.WriteToOriginalFile(s_backupOriginal);
                }
            }

            if (!any)
            {
                Verbose("No documents changed.");
            }
        }

        private static bool ParseArgs(ref string[] args, Dictionary<string, string> options)
        {
            int i;
            for (i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-console", StringComparison.OrdinalIgnoreCase))
                {
                    s_outputToConsole = true;
                }
                else if (args[i].Equals("-backup", StringComparison.OrdinalIgnoreCase))
                {
                    s_backupOriginal = true;
                }
                else if (args[i].Equals("-Xattach", StringComparison.OrdinalIgnoreCase))
                {
                    Debugger.Launch();
                }
                else if (args[i].Equals("-verbose", StringComparison.OrdinalIgnoreCase))
                {
                    s_verbose = true;
                }
                else if (args[i].Equals("-warnaserror", StringComparison.OrdinalIgnoreCase))
                {
                    s_warningAsErrors = true;
                }
                else if (args[i].StartsWith("-p:", StringComparison.OrdinalIgnoreCase))
                {
                    string arg = args[i].Substring("-p:".Length);
                    int pos = arg.IndexOf('=');
                    if (pos != -1)
                    {
                        string key = arg.Substring(0, pos);
                        string val = arg.Substring(pos + 1);
                        options.Add(arg.Substring(0, pos), arg.Substring(pos + 1));
                        Verbose($"Adding option '{key}' = '{val}'");
                    }
                    else
                    {
                        options.Add(arg, "true");
                        Verbose($"Adding option '{arg}' = 'true'");
                    }
                }
                else if (args[i] == "--" || !args[i].StartsWith("-", StringComparison.Ordinal))
                {
                    break;
                }
                else if (args[i].StartsWith("-", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            string[] xargs = new string[args.Length - i];
            Array.Copy(args, i, xargs, 0, xargs.Length);
            args = xargs;
            return true;
        }

        private static bool DumpDiagnostics(IEnumerable<TreeResult> results)
        {
            bool mustExit = false;
            foreach (var result in results)
            {
                if (DumpDiagnostics(result.Diagnostics.ToImmutableArray()))
                    mustExit = true;
            }

            return mustExit;
        }

        private static void Verbose(object obj)
        {
            if (s_verbose)
            {
                OutputDiag(DiagnosticSeverity.Hidden, obj);
            }
        }

        private static void Error(string str)
        {
            OutputDiag(DiagnosticSeverity.Error, str);
        }

        private static bool DumpDiagnostics(ImmutableList<WorkspaceDiagnostic> diagnostics)
        {
            bool mustExit = false;
            foreach (var diag in diagnostics)
            {
                if (diag.Kind < WorkspaceDiagnosticKind.Warning && !s_verbose)
                    continue;

                var severity = DiagnosticSeverity.Info;
                switch (diag.Kind)
                {
                    case WorkspaceDiagnosticKind.Failure:
                        severity = DiagnosticSeverity.Error;
                        break;
                    case WorkspaceDiagnosticKind.Warning:
                        severity = DiagnosticSeverity.Warning;
                        break;
                }

                OutputDiag(severity, diag);
                if (severity == DiagnosticSeverity.Error ||
                    severity == DiagnosticSeverity.Warning && s_warningAsErrors)
                    mustExit = true;
            }

            return mustExit;
        }

        private static bool DumpDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            bool mustExit = false;
            foreach (var diag in diagnostics)
            {
                if (diag.Severity < DiagnosticSeverity.Warning && !s_verbose)
                    continue;

                OutputDiag(diag.Severity, diag);
                if (diag.Severity == DiagnosticSeverity.Error ||
                    diag.Severity == DiagnosticSeverity.Warning && s_warningAsErrors)
                    mustExit = true;
            }

            return mustExit;
        }

        private static void OutputDiag(DiagnosticSeverity severity, object message)
        {
            var color = Console.ForegroundColor;
            var tw = Console.Out;
            try
            {
                switch (severity)
                {
                    case DiagnosticSeverity.Hidden:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                    case DiagnosticSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case DiagnosticSeverity.Warning:
                        tw = Console.Error;
                        if (s_warningAsErrors)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                        }

                        break;
                    case DiagnosticSeverity.Error:
                        tw = Console.Error;
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }

                tw.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = color;
            }
        }
    }
}