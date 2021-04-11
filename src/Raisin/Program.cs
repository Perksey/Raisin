using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Raisin.PluginSystem;
using Ultz.Extensions.Logging;

namespace Raisin
{
    class Program
    {
        internal static ILoggerProvider LoggerProvider = new UltzLoggerProvider
        {
            MessageFormat = "§7[{8}:{9}:{10}] [{1} §9{0}§7] §f{2}"
        };
        private static readonly ILogger Logger = LoggerProvider.CreateLogger("Core");
        internal static readonly Version Version = typeof(Program).Assembly.GetName().Version;
        static async Task Main(string[] args)
        {
            Console.WriteLine($"RAISIN - Static Site Generator - v{Version?.ToString(3)}");
            Console.WriteLine();
            await Executor.RunAsync(args[0]);
            Thread.Sleep(100); // TODO fix upstream
        }
    }
}