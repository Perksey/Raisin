using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class ItemText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}