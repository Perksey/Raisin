using System.Collections.Immutable;
using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public abstract class BaseXmlDocumentation
    {
        public abstract ITextualDocumentation? Summary { get; set; }
        public abstract ITextualDocumentation? Remarks { get; set; }
        public abstract ITextualDocumentation? SeeAlso { get; set; }
        public abstract ImmutableArray<ITextualDocumentation?> Examples { get; set; }
        public abstract string[] DeclarationLines { get; set; }
    }
}