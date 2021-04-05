using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class PlainText : ITextualDocumentation
    {
        public string Value { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}