using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Raisin.PluginSystem
{
    public static class PluginLoader
    {
        public static IEnumerable<string> LoadAndEnumerateUserFacingNamespaces(
            Func<SavedPlugin[]> savedPluginProvider,
            Func<SavedPlugin, Assembly?> getPlugin,
            ILoggerProvider? loggerProvider = null,
            ILogger? logger = null)
        {
            logger ??= loggerProvider?.CreateLogger("PluginLoader");
            var raisinVersion = typeof(PluginLoader).Assembly.GetName().Version ??
                                throw new InvalidOperationException("Couldn't retrieve Raisin version.");
            foreach (var plugin in savedPluginProvider())
            {
                var asm = getPlugin(plugin);
                if (asm is null)
                {
                    continue;
                }

                if (plugin.RaisinVersion.Major != raisinVersion.Major)
                {
                    logger.LogError($"Couldn't load plugin \"{plugin.Name}\" because it is not compatible with " +
                                    $"Raisin v{raisinVersion.ToString(3)}, only v{plugin.RaisinVersion.ToString(3)}");
                    continue;
                }
                
                var attr = asm.GetCustomAttribute<RaisinPluginAttribute>();
                if (attr is null)
                {
                    logger.LogError($"Couldn't load plugin \"{plugin.Name}\" because its assembly had no " +
                                      "RaisinPluginAttribute.");
                    continue;
                }

                if (plugin.RaisinVersion > raisinVersion)
                {
                    logger.LogWarning($"Plugin \"{plugin.Name}\" was built against a newer version of Raisin " +
                                      $"(v{plugin.RaisinVersion}). Consider updating the Raisin global tool.");
                }
                else if (raisinVersion > plugin.RaisinVersion)
                {
                    logger.LogWarning($"Plugin \"{plugin.Name}\" was built against an older version of Raisin " +
                                      $"(v{plugin.RaisinVersion}). Consider updating this plugin.");
                }
                
                logger.LogInformation($"Loaded {plugin.Name} v{plugin.Version}");
                foreach (var userFacingNamespace in attr.UserFacingNamespaces)
                {
                    yield return userFacingNamespace;
                }
            }
        }
    }
}