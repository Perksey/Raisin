using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class ItemText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}