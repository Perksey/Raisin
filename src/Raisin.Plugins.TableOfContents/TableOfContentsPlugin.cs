using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Raisin.Core;
using Raisin.PluginSystem;

[assembly: RaisinPlugin("Raisin.Plugins.TableOfContents")]

namespace Raisin.Plugins.TableOfContents
{
    public static class TableOfContentsPlugin
    {
        public static RaisinEngine WithTableOfContents(this RaisinEngine engine, params string[] tocGlobs)
        {
            var logger = engine.GetLoggerOrDefault<TableOfContentsModel>();
            var matcher = engine.CreateGlobMatcher();
            foreach (var tocGlob in tocGlobs)
            {
                matcher.AddInclude(tocGlob);
            }

            var rawTocModels = matcher.GetResultsInFullPath(engine.InputDirectory).Select(x =>
            {
                try
                {
                    // load all the ToC JSON file names we globbed.
                    return ((string, TableOfContentsElement?)?) (x,
                        JsonSerializer.Deserialize<TableOfContentsElement>(File.ReadAllText(x, Encoding.UTF8)));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Couldn't parse table of contents file \"{x}\" as JSON: {ex.Message}");
                    return null;
                }
            }).Where(x => x?.Item2 != null).SelectMany(x =>
            {
                // get the file name of the ToC JSON file and the ToC model within it
                var (file, model) = x!.Value;
                // all paths in the model are relative to the directory in which the toc is contained.
                // get the path of the ToC in the input directory, and walk relative to that.
                var tocBasePath = Path.GetDirectoryName(Path.GetRelativePath(
                    engine.InputDirectory ?? throw new InvalidOperationException("No input directory provided."),
                    file))!;
                // get all ToC entries
                return Walk(model!, tocBasePath, model!);
            }).Select(x => (x.Rel.PathFixup(), x.RootModel));
            var tocModels = Link(rawTocModels);

            return engine.WithRazorModelOverride((srcRel, @base) =>
            {
                var (_, baseModel) = @base;
                TableOfContentsElement? toc;
                switch (engine.UseCaseSensitivePaths)
                {
                    case true when tocModels.TryGetValue(srcRel, out toc):
                    {
                        var copy = toc.Clone();
                        var (_, root, child) = Walk(copy, toc.TocBasePath, copy).FirstOrDefault(x => x.Rel == srcRel);
                        child.IsActive = true;
                        return Task.FromResult<object>(new TableOfContentsModel
                            {BaseModel = baseModel, TableOfContentsRoot = root});
                    }
                    case false when (toc = tocModels
                        .FirstOrDefault(x => x.Key.Equals(srcRel, StringComparison.OrdinalIgnoreCase))
                        .Value) != null:
                    {
                        var copy = toc.Clone();
                        var (_, root, child) = Walk(copy, toc.TocBasePath, copy)
                            .FirstOrDefault(x => x.Rel.Equals(srcRel, StringComparison.OrdinalIgnoreCase));
                        child.IsActive = true;
                        return Task.FromResult<object>(new TableOfContentsModel
                            {BaseModel = baseModel, TableOfContentsRoot = root});
                    }
                    default:
                    {
                        return Task.FromResult(baseModel);
                    }
                }
            });
        }

        private static IEnumerable<(string Rel, TableOfContentsElement RootModel, TableOfContentsElement Model)> Walk(
            TableOfContentsElement child, string tocBasePath, TableOfContentsElement root)
        {
            child.TocBasePath = tocBasePath;
            if (child.Url is not null)
            {
                yield return (Path.Combine(tocBasePath, child.Url), root!, child);
            }
        }
    }
}