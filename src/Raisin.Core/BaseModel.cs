using System;
using System.IO;
using System.Threading.Tasks;
using RazorLight;
using RazorLight.Text;

namespace Raisin.Core
{
    public abstract class BaseModel
    {
        public RazorEngine? Razor { get; internal set; }
        public string? DestinationRel { get; internal set; }
        public RaisinEngine? Raisin => Razor?.Raisin;

        public string MakeRelativeToCurrentOutput(string path)
        {
            if (Raisin is null || DestinationRel is null)
            {
                throw new InvalidOperationException("Not a valid model context.");
            }

            if (Raisin.OutputDirectory is null)
            {
                throw new InvalidOperationException("No output directory specified.");
            }

            // get the path relative to the root, relative to the output file's directory
            return Path.GetRelativePath(
                Path.Combine(Raisin.OutputDirectory, Path.GetDirectoryName(DestinationRel) ?? string.Empty),
                Path.Combine(Raisin.OutputDirectory, path)).PathFixup();
        }

        public string MakeRelativeToOutputDirectory(string path)
        {
            if (Raisin is null || DestinationRel is null)
            {
                throw new InvalidOperationException("Not a valid model context.");
            }

            if (Raisin.OutputDirectory is null)
            {
                throw new InvalidOperationException("No output directory specified.");
            }

            // get the path relative to the output file's directory, relative to the root
            return Path.GetRelativePath(
                    Path.Combine(Raisin.OutputDirectory),
                    Path.Combine(Raisin.OutputDirectory, Path.GetDirectoryName(DestinationRel) ?? string.Empty, path))
                .PathFixup();
        }

        public async Task<IRawString> IncludeAsync(TemplatePage page, string srcRel) => page.Raw(
            await (Razor ?? throw new InvalidOperationException("Model not active.")).RenderAsync(
                Path.Combine(
                    (Raisin ?? throw new InvalidOperationException("Raisin engine not found.")).InputDirectory ??
                    throw new InvalidOperationException("No input directory provided."), srcRel), this));
    }
}