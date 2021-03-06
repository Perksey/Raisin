using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class ListText : ITextualDocumentation
    {
        public string? Type { get; set; }
        public List<ITextualDocumentation> Children { get; set; } = new();
    }
}