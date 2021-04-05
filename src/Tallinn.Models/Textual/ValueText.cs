using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class ValueText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}