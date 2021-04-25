using Tallinn.Models.Textual;

namespace Tallinn.Models.References
{
    public sealed class TypeReferenceDocumentation
    {
        public TypedCref Cref { get; set; }
        public ITextualDocumentation? Documentation { get; set; }
    }
}