using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class InlineCodeText : ITextualDocumentation
    {
        public List<ITextualDocumentation> Children { get; set; } = new();
    }
}