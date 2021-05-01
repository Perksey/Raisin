using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class ParagraphText : ITextualDocumentation
    {
        public List<ITextualDocumentation> Children { get; set; } = new();
    }
}