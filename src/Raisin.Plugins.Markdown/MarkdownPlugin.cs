using System;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Parsers;
using Microsoft.Extensions.Logging;
using Raisin.Core;
using Raisin.PluginSystem;

[assembly: RaisinPlugin("Raisin.Plugins.Markdown")]

namespace Raisin.Plugins.Markdown
{
    public static class MarkdownPlugin
    {
        private static readonly string[] _defaultGlobArray = {"**/*.md"};

        public static RaisinEngine WithMarkdown(this RaisinEngine engine, params string[] mdGlobs)
            => engine.WithMarkdown(static() => new MarkdownPipelineBuilder().UseAutoIdentifiers().Build());

        public static RaisinEngine WithMarkdown(this RaisinEngine engine, Func<MarkdownPipeline> pipeline,
            params string[] mdGlobs)
        {
            if ((mdGlobs?.Length ?? 0) == 0)
            {
                mdGlobs = _defaultGlobArray;
            }

            var excludes = mdGlobs.Where(x => x.StartsWith("!")).ToArray();
            var logger = engine.GetLoggerOrDefault(nameof(MarkdownPlugin));
            logger.LogInformation("Markdown plugin enabled.");
            var mdPipeline = pipeline();
            return mdGlobs.Where(static x => !x.StartsWith("!")).Aggregate
            (
                engine,
                (current, glob) => current.WithRazorGenerator
                (
                    glob,
                    async src => (
                        Path.Combine(Path.GetDirectoryName(src) ?? ".",
                                Path.GetFileNameWithoutExtension(src) + ".html")
                            .PathFixup(),
                        (BaseModel) new HtmlModel
                        {
                            Html = MarkdownParser.Parse
                            (
                                await File.ReadAllTextAsync(Path.Combine(
                                    current.InputDirectory ??
                                    throw new InvalidOperationException("No input directory provided."), src)),
                                mdPipeline
                            ).ToHtml(mdPipeline)
                        }
                    ).EnumerateOne(),
                    excludes
                )
            );
        }
    }
}