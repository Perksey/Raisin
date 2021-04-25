using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class TermText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}