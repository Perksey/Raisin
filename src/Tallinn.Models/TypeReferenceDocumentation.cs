using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public class TypeReferenceDocumentation
    {
        public TypedCref Cref { get; set; }
        public ITextualDocumentation? Documentation { get; set; }
    }
}