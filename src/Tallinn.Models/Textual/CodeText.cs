using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class CodeText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}