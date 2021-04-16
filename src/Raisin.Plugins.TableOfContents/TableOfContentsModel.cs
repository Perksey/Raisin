namespace Raisin.Plugins.TableOfContents
{
    public struct TableOfContentsModel
    {
        public TableOfContentsElement TableOfContentsRoot { get; internal set; }
        public object BaseModel { get; internal set; }
    }
}