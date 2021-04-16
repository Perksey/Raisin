using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;

namespace Raisin.Core
{
    public class RaisinEngine
    {
        private string? _inputDirectory;
        private string? _outputDirectory;

        public RaisinEngine()
        {
            Razor = new(() => new(this));
        }

        public string? InputDirectory
        {
            get => _inputDirectory;
            set => _inputDirectory = value is not null ? Path.GetFullPath(value) : null;
        }

        public string? OutputDirectory
        {
            get => _outputDirectory;
            set => _outputDirectory = value is not null ? Path.GetFullPath(value) : null;
        }

        public string? RazorRoot { get; set; }
        public Lazy<RazorEngine> Razor { get; }
        public bool UseCaseSensitivePaths { get; set; }

        /// <summary>
        /// A concurrent dictionary where the key is the path of a source file relative to the
        /// <see cref="InputDirectory"/>, and the value is an asynchronous method returning the desired output path of
        /// the HTML bytes, as well as the actual HTML bytes.
        /// </summary>
        /// <remarks>
        /// This should only be populated using the With methods.
        /// </remarks>
        public ConcurrentDictionary<string, Func<Task<IEnumerable<(string OutputPath, byte[] Data)>>>> Outputs { get; }
            = new();

        /// <summary>
        /// A stack of asynchronous "model overrides" - that is, a method which receives the desired output path and
        /// proposed model, and returns a wrapped or overriden model.
        /// </summary>
        /// <remarks>
        /// This should only be populated using the With methods.
        /// </remarks>
        public ConcurrentStack<Func<string, (string OutputPath, object OriginalModel), Task<object>>> ModelOverrides
        {
            get;
        } = new();

        /// <summary>
        /// The logger provider used for Raisin generation.
        /// </summary>
        public ILoggerProvider? LoggerProvider { get; set; }

        /// <summary>
        /// The logger used for this instance and, if a <see cref="LoggerProvider"/> has not been provided, the rest of
        /// Raisin generation.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Retrieves a logger for the given type if this Raisin generator is configured for logging, or null otherwise. 
        /// </summary>
        /// <typeparam name="T">The type to get a logger for.</typeparam>
        /// <returns>A logger, or null if this generator is not configured for logging.</returns>
        public ILogger? GetLoggerOrDefault<T>() => GetLoggerOrDefault(typeof(T).Name);

        /// <summary>
        /// Retrieves a logger with the given name if this Raisin generator is configured for logging, or null
        /// otherwise. 
        /// </summary>
        /// <typeparam name="T">The type to get a logger for.</typeparam>
        /// <returns>A logger, or null if this generator is not configured for logging.</returns>
        public ILogger? GetLoggerOrDefault(string name) => LoggerProvider?.CreateLogger(name) ?? Logger;

        /// <summary>
        /// Returns a glob pattern matcher which is either case sensitive or case insensitive, dependent on the value of
        /// the <see cref="UseCaseSensitivePaths"/> property.
        /// </summary>
        /// <returns>A glob pattern matcher for the current configuration.</returns>
        public Matcher CreateGlobMatcher()
            => new(UseCaseSensitivePaths ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

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

        /// <summary>
        /// Sets the Razor file which receives all Razor models.
        /// </summary>
        /// <param name="file">The Razor file which receives the models, relative to the <see cref="InputDirectory"/></param>
        /// <returns></returns>
        public RaisinEngine WithRazorRoot(string file)
        {
            RazorRoot = file;
            return this;
        }

        /// <summary>
        /// Registers a Razor generator for the files matches by the given glob pattern, which takes an input file, and
        /// asynchronously returns an enumerable of desired output paths and models to feed into the Razor engine.
        /// </summary>
        /// <param name="glob">The glob pattern for the files to which this Razor generator applies, relative to the <see cref="InputDirectory"/></param>
        /// <param name="generator">The output Razor model generator to use.</param>
        /// <returns>This instance, for chaining purposes.</returns>
        public RaisinEngine WithRazorGenerator(string glob,
            Func<string, Task<IEnumerable<(string OutputPath, object Model)>>> generator)
        {
            var matcher = CreateGlobMatcher();
            matcher.AddInclude(glob);
            foreach (var file in matcher.GetResultsInFullPath(InputDirectory))
            {
                var rel = Path.GetRelativePath(
                        InputDirectory ?? throw new InvalidOperationException("No input directory specified."), file)
                    .PathFixup();
                if (Outputs.TryAdd(rel,
                    async () => await Task.WhenAll((await RunGeneratorAsync(rel)).Select(async x =>
                        (x.OutputPath, await Razor.Value.BuildFileAsync(x.Model))))))
                {
                    continue;
                }

                if (Outputs.ContainsKey(rel))
                {
                    Logger?.LogWarning($"Couldn't add Razor generator for \"{file}\" because this source has " +
                                       "already been consumed by another generator.");
                }
                else
                {
                    Logger?.LogWarning($"Couldn't add Razor generator for \"{file}\" due to an unknown error.");
                }
            }

            return this;

            async Task<IEnumerable<(string OutputPath, object Model)>> RunGeneratorAsync(string rel)
            {
                var result = (await generator(rel)).Select(x => (OutputPath: x.OutputPath.PathFixup(), x.Model));
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var modelOverride in ModelOverrides)
                {
                    result = await Task.WhenAll(result.Select(async x => (x.OutputPath, await modelOverride(rel, x))));
                }

                return result;
            }
        }

        /// <summary>
        /// Registers a function which, given a source file path (relative to the <see cref="InputDirectory"/>),
        /// desired output path and base model; wraps and/or overrides the Razor model used in output files.
        /// </summary>
        /// <param name="modelOverride">The function that overrides Razor models used for generating output.</param>
        /// <returns>This instance, for chaining purposes.</returns>
        public RaisinEngine WithRazorModelOverride(
            Func<string, (string OutputPath, object OriginalModel), Task<object>> modelOverride)
        {
            ModelOverrides.Push(modelOverride);
            return this;
        }

        public RaisinEngine WithCaseSensitivePaths(bool useCaseSensitivePaths)
        {
            UseCaseSensitivePaths = useCaseSensitivePaths;
            return this;
        }

        public RaisinEngine WithLoggerProvider(ILoggerProvider loggerProvider)
        {
            LoggerProvider = loggerProvider;
            Logger = GetLoggerOrDefault<RaisinEngine>();
            return this;
        }

        public RaisinEngine WithLogger(ILogger logger)
        {
            Logger = logger;
            return this;
        }

        /// <summary>
        /// Registers all files matched by the given glob patterns (executed relative to the
        /// <see cref="InputDirectory" />) to be copied directly to the output, with no specific further generation
        /// logic to be applied.
        /// </summary>
        /// <remarks>
        /// This function preserves directory structure in addition to all contents.
        /// </remarks>
        /// <param name="globs">The glob patterns to copy to the output directory.</param>
        /// <returns>This instance, for chaining purposes.</returns>
        public RaisinEngine WithPreservations(params string[] globs)
        {
            var matcher = CreateGlobMatcher();
            foreach (var glob in globs)
            {
                matcher.AddInclude(glob);
            }

            foreach (var file in matcher.GetResultsInFullPath(InputDirectory))
            {
                var rel = Path.GetRelativePath(
                    InputDirectory ?? throw new InvalidOperationException("No input directory specified."), file);
                if (Outputs.TryAdd(rel, async () =>
                {
                    await using var stream = File.OpenRead(file);
                    await using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    return (rel, memoryStream.ToArray()).EnumerateOne();
                }))
                {
                    continue;
                }

                if (Outputs.ContainsKey(rel))
                {
                    Logger?.LogWarning($"Couldn't add preservation for \"{file}\" because this source has " +
                                       "already been consumed by another generator.");
                }
                else
                {
                    Logger?.LogWarning($"Couldn't add preservation for \"{file}\" due to an unknown error.");
                }
            }

            return this;
        }

        /// <summary>
        /// Asynchronously processes all input files, generates outputs, and writes all outputs to the
        /// <see cref="OutputDirectory"/>.
        /// </summary>
        /// <returns>An asynchronous task.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a parameter is missing, such as the <see cref="OutputDirectory"/>.
        /// </exception>
        public async Task GenerateAsync()
        {
            if (OutputDirectory is null)
            {
                throw new InvalidOperationException("No output directory specified.");
            }

            if (Directory.GetFiles(OutputDirectory.CreateDirectoryIfNeeded(), "*", SearchOption.TopDirectoryOnly).Any())
            {
                Logger?.LogWarning("It's generally recommended to have a clean output directory, as Raisin will not " +
                                   "delete old files.");
            }

            // Key = Lower Case Name, Value = (Destination File, Source File, Data)
            var fileMap = new ConcurrentDictionary<string, (string To, string From, bool Done)>();
            await Task.WhenAll(Outputs.Select(async x =>
            {
                IEnumerable<(string OutputPath, byte[] Data)> generated;
                try
                {
                    generated = await x.Value();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"Failed to generate pages for \"{x.Key}\". {ex}");
                    return;
                }

                foreach (var (dest, data) in generated)
                {
                    var actual = dest.PathFixup();
                    var key = actual;
                    if (!UseCaseSensitivePaths)
                    {
                        key = key.ToLower();
                    }

                    var tuple = (To: dest, From: x.Key.PathFixup(), Done: false);
                    if (!fileMap.TryAdd(key, tuple))
                    {
                        if (fileMap.TryGetValue(key, out var val))
                        {
                            Logger?.LogWarning("Duplicate generated file detected.");
                            Logger?.LogWarning($"Existing: \"{val.From}\" -> \"{val.To}\"");
                            Logger?.LogWarning($"New: \"{x.Key.PathFixup()}\" -> \"{dest}\"");
                            if (dest != val.To && !UseCaseSensitivePaths)
                            {
                                Logger?.LogWarning("Case sensitive paths are currently disabled, so paths that " +
                                                   "differ only by case will be treated as duplicates (primarily for " +
                                                   " Windows compatibility). Use WithCaseSensitivePaths(false) to " +
                                                   "disable this.");
                            }
                        }
                        else
                        {
                            Logger?.LogWarning($"Unknown error when registering file {x.Key.PathFixup()} -> {dest}");
                        }
                    }
                    else
                    {
                        try
                        {
                            await using var stream = File.OpenWrite(Path.Combine(OutputDirectory, dest).CreateFileDirectoryIfNeeded());
                            await stream.WriteAsync(data);
                            await stream.FlushAsync();
                            if (!fileMap.TryUpdate(key, (tuple.To, tuple.From, true), tuple))
                            {
                                Logger?.LogWarning($"\"{x.Key.PathFixup()}\" -> \"{dest}\" complete but failed to " +
                                                   "report status - it may be wrongfully reported as incomplete!");
                            }
                            else
                            {
                                Logger?.LogInformation($"\"{x.Key.PathFixup()}\" -> \"{dest}\"");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, $"Failed to write \"{x.Key.PathFixup()}\" -> \"{dest}\". {ex}");
                        }
                    }
                }
            }));
        }
    }
}