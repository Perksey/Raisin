namespace Tallinn.Models.Textual
{
    public class TypedCref : Cref<TypeDocumentation>
    {
        public int IndirectionLevels { get; set; }
        public TypedCrefFlags Flags { get; set; }
    }
}