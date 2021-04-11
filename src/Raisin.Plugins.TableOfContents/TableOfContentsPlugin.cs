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

            var tocModels = matcher.GetResultsInFullPath(engine.InputDirectory).Select(x =>
            {
                try
                {
                    return ((string, TocElementModel?)?) (x,
                        JsonSerializer.Deserialize<TocElementModel>(File.ReadAllText(x, Encoding.UTF8)));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Couldn't parse table of contents file as JSON: \"{x}\"");
                    return null;
                }
            }).Where(x => x?.Item2 != null).SelectMany(x =>
            {
                var (file, model) = x!.Value;
                var tocBasePath = Path.GetDirectoryName(Path.GetRelativePath(
                    engine.InputDirectory ?? throw new InvalidOperationException("No input directory provided."),
                    file))!;
                return Walk(model!, tocBasePath, model!);
            }).ToDictionary(x => x.Rel.PathFixup(), x => x.RootModel);

            return engine.WithRazorModelOverride((srcRel, @base) =>
            {
                var (_, baseModel) = @base;
                TocElementModel? toc;
                switch (engine.UseCaseSensitivePaths)
                {
                    case true when tocModels.TryGetValue(srcRel, out toc):
                    {
                        var copy = toc.Clone();
                        var (_, root, child) = Walk(copy, toc.TocBasePath, copy).FirstOrDefault(x => x.Rel == srcRel);
                        child.IsActive = true;
                        return Task.FromResult<object>(new TableOfContentsModel
                            {BaseModel = baseModel, TocRoot = root});
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
                            {BaseModel = baseModel, TocRoot = root});
                    }
                    default:
                    {
                        return Task.FromResult(baseModel);
                    }
                }
            });
        }

        private static IEnumerable<(string Rel, TocElementModel RootModel, TocElementModel Model)> Walk(
            TocElementModel child, string tocBasePath, TocElementModel rootModel)
        {
            child.TocBasePath = tocBasePath;
            if (child.Url is not null)
            {
                yield return (Path.Combine(tocBasePath, child.Url), rootModel!, child);
            }
        }
    }
}