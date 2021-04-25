using System.Collections.Generic;
using Tallinn.Models.References;

namespace Tallinn.Models.Textual
{
    public sealed class SeeText : ITextualDocumentation
    {
        public Cref? Cref { get; set; }
        public string? Href { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}