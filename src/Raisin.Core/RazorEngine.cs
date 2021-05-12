using System;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RazorLight;
using RazorLight.Extensions;

namespace Raisin.Core
{
    /// <summary>
    /// Contains logic for compiling Razor pages into HTML bytes.
    /// </summary>
    public class RazorEngine
    {
        /// <summary>
        /// The Raisin engine owning this instance.
        /// </summary>
        public RaisinEngine Raisin { get; }

        /// <summary>
        /// The underlying RazorLight engine.
        /// </summary>
        public Lazy<RazorLightEngine> Razor { get; }

        /// <summary>
        /// The logger used by this instance.
        /// </summary>
        private ILogger? Logger { get; }

        /// <summary>
        /// Creates a Razor engine for the given Raisin engine.
        /// </summary>
        /// <param name="raisin">The Raisin engine instance owning this instance.</param>
        public RazorEngine(RaisinEngine raisin)
        {
            Raisin = raisin;
            Logger = raisin.GetLoggerOrDefault<RazorEngine>();
            Razor = new(() =>
            {
                var ret = new RazorLightEngineBuilder()
                    .UseFileSystemProject(Raisin.InputDirectory)
                    .UseMemoryCachingProvider()
                    .AddMetadataReferences(Raisin.RazorMetadataReferences
                        .Select(x => (MetadataReference) MetadataReference.CreateFromFile(x.Location)).ToArray())
                    .Build();
                Logger?.LogInformation("Created Razor Light engine.");
                return ret;
            });
        }

        /// <summary>
        /// Compiles the Razor root using the given model.
        /// </summary>
        /// <param name="model">The model to compile the Razor root using.</param>
        /// <returns>HTML bytes.</returns>
        public async Task<byte[]> BuildFileAsync(BaseModel model, string outputPath)
        {
            model.Razor = this;
            model.DestinationRel = outputPath;
            return Encoding.UTF8.GetBytes(
                await RenderAsync(Raisin.RazorRoot ?? throw new InvalidOperationException("No Razor root exists."),
                    model));
        }

        internal async Task<string> RenderAsync(string file, object model)
        {
            try
            {
                return await Razor.Value.CompileRenderAsync(file, model);
            }
            catch
            {
                Logger?.LogError($"Rendering failed for \"{file}\".");
                throw;
            }
        }
    }
}