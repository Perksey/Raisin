using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class ValueText : ITextualDocumentation
    {
        public List<ITextualDocumentation> Children { get; set; } = new();
    }
}