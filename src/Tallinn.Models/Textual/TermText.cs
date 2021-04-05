using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class TermText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}