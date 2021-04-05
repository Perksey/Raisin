using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class InlineCodeText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}