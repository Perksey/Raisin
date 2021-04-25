namespace Raisin.Core
{
    public abstract class BaseModel
    {
        public RazorEngine? Razor { get; internal set; }
        public RaisinEngine? Raisin => Razor?.Raisin;
    }
}