using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class SeeText : ITextualDocumentation
    {
        public Cref? Cref { get; set; }
        public string? Href { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}