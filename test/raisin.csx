await Raisin
    .WithInputDirectory(".")
    .WithOutputDirectory("_output")
    .WithRazorRoot("_theme/root.cshtml")
    .WithMarkdown()
    .WithTableOfContents()
    .WithForceCleanOutput()
    .GenerateAsync(); 

// Unsafe testing code
var roots = ((ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)>) Raisin.InterPluginState["Raisin.Plugins.TableOfContents.TableOfContentsPlugin.TocDict"]).Select(x => x.Value.Root).Distinct();

int indentationLevel = 0;
var logger = Raisin.GetLoggerOrDefault("JustTesting");
void EnumerateChildren(IEnumerable<TableOfContentsElement> roots)
{
    foreach (var value in roots)
    {
        logger?.LogDebug(new string(' ', indentationLevel * 4) + value.Name + " (" + value.Href + ", parent " + value.Parent?.Href + ")");
        indentationLevel++;
        EnumerateChildren(value.Children ?? Enumerable.Empty<TableOfContentsElement>());
        indentationLevel--;
    }
}

EnumerateChildren(roots);
logger?.LogDebug(string.Join(", ", ((ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)>)Raisin.InterPluginState["Raisin.Plugins.TableOfContents.TableOfContentsPlugin.TocDict"]).Select(x => x.Key)));