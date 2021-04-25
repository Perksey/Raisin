using System;
using System.IO;
using System.Linq;
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
        {
            if ((mdGlobs?.Length ?? 0) == 0)
            {
                mdGlobs = _defaultGlobArray;
            }

            var logger = engine.GetLoggerOrDefault(nameof(MarkdownPlugin));
            logger.LogInformation("Markdown plugin enabled.");
            return mdGlobs.Aggregate
            (
                engine,
                static (current, glob) => current.WithRazorGenerator
                (
                    glob,
                    async src =>
                    (
                        Path.Combine(Path.GetDirectoryName(src) ?? ".", Path.GetFileNameWithoutExtension(src) + ".html")
                            .PathFixup(),
                        (BaseModel) new HtmlModel
                        {
                            Html = Markdig.Markdown.ToHtml(await File.ReadAllTextAsync(Path.Combine(
                                current.InputDirectory ??
                                throw new InvalidOperationException("No input directory provided."), src)))
                        }
                    ).EnumerateOne()
                )
            );
        }
    }
}