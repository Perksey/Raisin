using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class ParagraphText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}