using Raisin.Core;

namespace Raisin.Plugins.TableOfContents
{
    public class TableOfContentsModel : BaseModel
    {
        public TableOfContentsElement Root { get; internal set; }
        public TableOfContentsElement Node { get; internal set; }
        public BaseModel BaseModel { get; internal set; }
    }
}