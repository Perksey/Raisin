namespace Tallinn.Models.References
{
    public sealed class TypedCref : Cref
    {
        public int IndirectionLevels { get; set; }
        public TypedCrefFlags Flags { get; set; }
    }
}