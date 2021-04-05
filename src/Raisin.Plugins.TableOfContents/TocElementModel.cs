using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace Raisin.Plugins.TableOfContents
{
    public class TocElementModel : ICloneable
    {
        /// <summary>
        /// The name of this page.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The URL to the file referenced in the table of contents, relative to the table of contents file.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Url { get; internal set; }

        /// <summary>
        /// The elements beneath this element in the table of contents.
        /// </summary>
        public ImmutableArray<TocElementModel> Children { get; internal set; }

        /// <summary>
        /// Whether the <see cref="TableOfContentsModel"/> containing this element is being passed to the page
        /// represented by this element.
        /// </summary>
        [JsonIgnore]
        public bool IsActive { get; internal set; }

        /// <summary>
        /// Whether any of the direct descendants in <see cref="Children"/> of this table of contents element are
        /// active, as defined by <see cref="IsActive"/>.
        /// </summary>
        /// <remarks>
        /// i.e. this method returns true when one of this element's children is active, but false when one of the
        /// children's children are active. To return true even in the latter case, use <see cref="IsAnyChildActive"/>.
        /// </remarks>
        [JsonIgnore]
        public bool IsChildActive => Children.Any(static x => x.IsActive);

        /// <summary>
        /// Whether any of the descendants in <see cref="Children"/> of this table of contents element are
        /// active, as defined by <see cref="IsActive"/>.
        /// </summary>
        /// <remarks>
        /// i.e. this method returns true when one of this element's children is active, even when one of the
        /// children's children are active, regardless of the depth of the descendant. To return false in the latter
        /// case, use <see cref="IsChildActive"/>.
        /// </remarks>
        [JsonIgnore]
        public bool IsAnyChildActive => Children.Any(static x => x.IsActive || x.IsAnyChildActive);
        
        [JsonIgnore]
        internal string TocBasePath { get; set; }

        object ICloneable.Clone() => Clone();

        public TocElementModel Clone() => new()
        {
            Name = Name,
            Url = Url,
            Children = Children.Select(x => x.Clone()).ToImmutableArray(),
            IsActive = IsActive,
            TocBasePath = TocBasePath
        };
    }
}