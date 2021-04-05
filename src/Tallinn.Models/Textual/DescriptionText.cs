using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class DescriptionText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}