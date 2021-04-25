using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class CodeText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}