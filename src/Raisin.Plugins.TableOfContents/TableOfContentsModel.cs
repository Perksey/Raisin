namespace Raisin.Plugins.TableOfContents
{
    public struct TableOfContentsModel
    {
        public TocElementModel TocRoot { get; internal set; }
        public object BaseModel { get; internal set; }
    }
}