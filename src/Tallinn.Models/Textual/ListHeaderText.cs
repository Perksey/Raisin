using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class ListHeaderText : ITextualDocumentation
    {
        public List<ITextualDocumentation> Children { get; set; } = new();
    }
}