namespace Raisin.Plugins.TableOfContents
{
    public class TableOfContentsModel
    {
        public TableOfContentsElement TocRoot { get; internal set; }
        public TableOfContentsElement TocNode { get; internal set; }
        public object BaseModel { get; internal set; }
    }
}