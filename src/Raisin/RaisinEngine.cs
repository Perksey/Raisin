using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Raisin
{
    public class RaisinEngine
    {
        public string? InputDirectory { get; set; }
        public string? OutputDirectory { get; set; }
        public string? RazorRoot { get; set; }
        public ConcurrentDictionary<string, Func<string, IEnumerable<(string OutputPath, object Model)>>> PageGenerators { get; set; } = new();

        public RaisinEngine WithInputDirectory(string inputDir)
        {
            InputDirectory = inputDir;
            return this;
        }

        public RaisinEngine WithOutputDirectory(string outputDir)
        {
            OutputDirectory = outputDir;
            return this;
        }

        public RaisinEngine WithRazorRoot(string file)
        {
            RazorRoot = file;
            return this;
        }

        public RaisinEngine WithPageGenerator(string glob, Func<string, IEnumerable<(string OutputPath, object Model)>> generator)
        {
            var matcher = new Matcher();
            matcher.AddInclude(glob);
            foreach (var file in matcher.GetResultsInFullPath(InputDirectory))
            {
                var rel = Path.GetRelativePath(InputDirectory, file);
                if (PageGenerators.ContainsKey(rel))
                {
                    // TODO log warning of duplicate match
                    continue;
                }

                PageGenerators.TryAdd(rel, generator); // TODO log on fail
            }
            return this;
        }

        public async Task<RaisinEngine> GenerateAsync()
        {
            throw new NotImplementedException();
        }
    }
}