using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Raisin.PluginSystem
{
    public static class PluginLoader
    {
        public static readonly string[] DefaultPackageFeeds = {"https://api.nuget.org/v3/index.json"};
        public static async IAsyncEnumerable<string> LoadAndEnumerateUserFacingNamespacesAsync(
            string workDir,
            IEnumerable<string> directives,
            ILoggerProvider? loggerProvider = null,
            ILogger? logger = null)
        {
            logger ??= loggerProvider?.CreateLogger(nameof(PluginLoader));
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }

            var packagePath = Path.Combine(workDir, "packages");
            if (!Directory.Exists(packagePath))
            {
                Directory.CreateDirectory(packagePath);
            }

            var packageFeeds = new List<string>();
            await foreach (var asm in directives.ToAsyncEnumerable().SelectMany(x =>
            {
                var splitDirective = x[1..].Split(' ');
                if (splitDirective[0].ToLower() == "package" && splitDirective.Length > 1)
                {
                    return NuGetDownloader.DownloadAsync(splitDirective[1],
                        splitDirective.Length > 2 ? splitDirective[2] : null, packagePath,
                        packageFeeds.Count > 0 ? packageFeeds.ToArray() : DefaultPackageFeeds,
                        loggerProvider?.CreateLogger(nameof(NuGetDownloader)) ?? logger);
                }
                else if (splitDirective[0].ToLower() == "feed" && splitDirective.Length > 1)
                {
                    packageFeeds.Add(string.Join(" ", splitDirective.Skip(1)));
                }
                else if (splitDirective[0].ToLower() == "reference" && splitDirective.Length > 1)
                {
                    async IAsyncEnumerable<Assembly> AsmEnumerable()
                    {
                        yield return await Task.FromResult(
                            Assembly.Load(new AssemblyName(string.Join(" ", splitDirective.Skip(1)))));
                    }

                    return AsmEnumerable();
                }

                return AsyncEnumerable.Empty<Assembly>();
            }))
            {
                var attr = asm.GetCustomAttribute<RaisinPluginAttribute>();
                if (attr is null)
                {
                    continue;
                }

                var asmName = asm.GetName();
                logger?.LogInformation($"{asmName.Name} v{asmName.Version?.ToString(3)} loaded.");
                foreach (var userFacingNamespace in attr.UserFacingNamespaces)
                {
                    yield return userFacingNamespace;
                }
            }
        }
    }
}