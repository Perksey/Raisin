using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RazorLight;

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
                Logger?.LogInformation("Creating Razor Light engine...");
                return new RazorLightEngineBuilder()
                    .UseFileSystemProject(Raisin.InputDirectory)
                    .UseMemoryCachingProvider()
                    .Build();
            });
        }

        /// <summary>
        /// Compiles the Razor root using the given model.
        /// </summary>
        /// <param name="model">The model to compile the Razor root using.</param>
        /// <returns>HTML bytes.</returns>
        public async Task<byte[]> BuildFileAsync(object model)
            => Encoding.UTF8.GetBytes(await Razor.Value.CompileRenderAsync(Raisin.RazorRoot, model));
    }
}