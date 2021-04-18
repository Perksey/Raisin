using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ultz.Extensions.Logging;

namespace Tallinn
{
    class Program
    {
        internal static UltzLoggerProvider LoggerProvider = new UltzLoggerProvider
        {
            MessageFormat = "§7[{5:HH}:{5:mm}:{5:ss}] [{1} §9{0}§7] §f{2}"
        };
        private static readonly ILogger Logger = LoggerProvider.CreateLogger("Core");
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("TALLINN - C# Documentation and Metadata Scraper - " +
                              $"v{typeof(Program).Assembly.GetName().Version?.ToString(3)}");
            Console.WriteLine();
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    new []{"--input", "-i"},
                    "The input sln to scrape documentation from"),
                new Option<FileInfo>(
                    new []{"--output", "-o"},
                    "The output Tallinn API file (usually with extension .tallinn)"),
                new Option<string>(
                    new[]{"--version", "-v"},
                    () => "latest",
                    "The version for this API.")
            };

            rootCommand.Description = null;

            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>(Generator.CommandHandler);

            var ret = await rootCommand.InvokeAsync(args);
            LoggerProvider.WaitAndShutdown();
            return ret;
        }
    }
}