using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class DescriptionText : ITextualDocumentation
    {
        public List<ITextualDocumentation>? Children { get; set; }
    }
}