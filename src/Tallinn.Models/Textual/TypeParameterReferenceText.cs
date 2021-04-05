using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class TypeParameterReferenceText : ITextualDocumentation
    {
        public string Name { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}