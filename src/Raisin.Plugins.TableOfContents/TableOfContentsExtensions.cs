using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Raisin.Core;

namespace Raisin.Plugins.TableOfContents
{
    public static class TableOfContentsExtensions
    {
        public static bool TryFindAnyTableOfContents(this RaisinEngine raisin,
            [NotNullWhen(true)] out TableOfContentsElement? root)
        {
            root = null;
            return !(!raisin.InterPluginState.TryGetValue(TableOfContentsPlugin.TocDict, out var rawTocDict) ||
                     rawTocDict is not ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement
                         Value)> tocDict) && (root = tocDict.FirstOrDefault().Value.Root) is not null;
        }

        public static bool TryFindAnyTableOfContents(this BaseModel model, out TableOfContentsElement? root)
            => (model.Raisin ??
                throw new ArgumentException("Model not active (does not have a Raisin engine attached)"))
                .TryFindAnyTableOfContents(out root);
    }
}