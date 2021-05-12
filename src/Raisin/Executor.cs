using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using Raisin.Core;
using Raisin.PluginSystem;
using Ultz.Extensions.Logging;

namespace Raisin
{
    public static class Executor
    {
        private static readonly string[] _defaultImports =
        {
            "System",
            "System.Collections.Generic",
            "System.Collections.Concurrent",
            "System.Linq",
            "System.Text",
            "System.Diagnostics",
            "System.IO",
            "System.Threading",
            "System.Threading.Tasks",
            "Microsoft.Extensions.Logging",
            "Raisin.Core"
        };

        private static readonly string[] _bundledPluginDirectives =
        {
            "#reference Raisin.Plugins.Markdown",
            "#reference Raisin.Plugins.TableOfContents"
        };

        public static async Task RunAsync(string inputFile)
        {
            if (inputFile is null || !File.Exists(inputFile))
            {
                throw new ArgumentNullException(nameof(inputFile), "File parameter is null or does not exist.");
            }

            var codeLines = ("public static async Task Script(RaisinEngine Raisin)\n{\n" +
                             await File.ReadAllTextAsync(inputFile) +
                             "\n}\nreturn (Func<RaisinEngine, Task>) Script;").Split('\n');
            var code = string.Join('\n', codeLines.Where(x => !x.StartsWith("#")));
            var directives = _bundledPluginDirectives.Concat(codeLines.Where(x => x.StartsWith("#")));
            var restoreEnv = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile)!)!;
            var logger = Program.LoggerProvider.CreateLogger("Executor");
            var userFacingNamespaces = await PluginLoader.LoadAndEnumerateUserFacingNamespacesAsync(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Raisin"),
                directives, Program.LoggerProvider).ToArrayAsync();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
                .Concat(AssemblyLoadContext.Default.Assemblies.Where(x =>
                    !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
                .Distinct()
                .ToArray();

            var sopts = ScriptOptions.Default
                .WithImports(_defaultImports.Concat(userFacingNamespaces).ToArray())
                .WithReferences(assemblies);
            try
            {
                logger.LogInformation("Collecting...");
                var result = await CSharpScript.EvaluateAsync(code, sopts);
                if (result is not Func<RaisinEngine, Task> @delegate)
                {
                    logger.LogError("Unknown error (result is not Func<RaisinEngine, Task>)");
                    return;
                }

                logger.LogInformation("Generation started.");
                var sw = Stopwatch.StartNew();
                await @delegate(new RaisinEngine().WithLoggerProvider(Program.LoggerProvider)
                    .WithRazorMetadataReferences(assemblies));
                logger.LogInformation($"Generation finished in {sw.Elapsed.TotalSeconds:R} seconds.");
            }
            catch (CompilationErrorException e)
            {
                logger.LogError("Script compilation failed.");
                foreach (var diagnostic in e.Diagnostics)
                {
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (diagnostic.Severity)
                    {
                        case DiagnosticSeverity.Info:
                        {
                            logger.LogInformation($"{diagnostic.Id} {diagnostic.GetMessage()}");
                            break;
                        }
                        case DiagnosticSeverity.Warning:
                        {
                            logger.LogWarning($"{diagnostic.Id} {diagnostic.GetMessage()}");
                            break;
                        }
                        case DiagnosticSeverity.Error:
                        {
                            logger.LogError($"{diagnostic.Id} {diagnostic.GetMessage()}");
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Generation failed. {e}");
            }
            finally
            {
                Environment.CurrentDirectory = restoreEnv;
            }
        }
    }
}