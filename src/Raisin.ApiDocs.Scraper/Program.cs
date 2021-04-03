using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ultz.Extensions.Logging;

namespace Raisin.ApiDocs.Scraper
{
    class Program
    {
        internal static ILoggerProvider LoggerProvider = new UltzLoggerProvider
        {
            MessageFormat = "§7[{8}:{9}:{10}] [{1} §9{0}§7] §f{2}"
        };
        private static readonly ILogger Logger = LoggerProvider.CreateLogger("Core");
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("RAISIN - Documentation Scraper - " +
                              $"v{typeof(Program).Assembly.GetName().Version?.ToString(3)}");
            Console.WriteLine();
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    new []{"--input", "-i"},
                    "The input sln to scrape documentation from"),
                new Option<FileInfo>(
                    new []{"--output", "-o"},
                    "The output raisin file.")
            };

            rootCommand.Description = null;

            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>(Generator.CommandHandler);

            var ret = await rootCommand.InvokeAsync(args);
            Thread.Sleep(100); // TODO fix threading issues upstream
            return ret;
        }
    }
}