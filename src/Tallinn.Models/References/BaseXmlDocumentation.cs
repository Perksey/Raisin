using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models.References
{
    public abstract class BaseXmlDocumentation
    {
        public abstract ITextualDocumentation? Summary { get; set; }
        public abstract ITextualDocumentation? Remarks { get; set; }
        public abstract List<ITextualDocumentation?> SeeAlso { get; set; }
        public abstract List<ITextualDocumentation?> Examples { get; set; }
        public abstract List<string> DeclarationLines { get; set; }
    }
}