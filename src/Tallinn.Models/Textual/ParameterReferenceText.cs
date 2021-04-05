using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class ParameterReferenceText : ITextualDocumentation
    {
        public string Name { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}