using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public static async Task RunAsync(string inputFile)
        {
            if (inputFile is null || !File.Exists(inputFile))
            {
                throw new ArgumentNullException(nameof(inputFile), "File parameter is null or does not exist.");
            }

            var code = "public static async Task Script(RaisinEngine Raisin)\n{\n" +
                       await File.ReadAllTextAsync(inputFile) + "\n}\nreturn (Func<RaisinEngine, Task>) Script;";
            Environment.CurrentDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile)!)!;
            var logger = Program.LoggerProvider.CreateLogger("Executor");
            var userFacingNamespaces = PluginLoader.LoadAndEnumerateUserFacingNamespaces(() => new[]
            {
                new SavedPlugin {Name = "Raisin.Plugins.Markdown", Version = Program.Version, RaisinVersion = Program.Version},
                new SavedPlugin {Name = "Raisin.Plugins.TableOfContents", Version = Program.Version, RaisinVersion = Program.Version},
            }, x => Assembly.Load(x.Name), Program.LoggerProvider);

            var sopts = ScriptOptions.Default
                .WithImports(_defaultImports.Concat(userFacingNamespaces).ToArray())
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));
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
                await @delegate(new RaisinEngine().WithLoggerProvider(Program.LoggerProvider));
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
                // TODO reminder for a breaking change upstream to move the formatter delegate invocation
                logger.LogError(e, $"Generation failed. {e}");
            }
        }
    }
}