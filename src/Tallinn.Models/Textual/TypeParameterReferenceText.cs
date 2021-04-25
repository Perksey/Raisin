using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public sealed class TypeParameterReferenceText : ITextualDocumentation
    {
        public string Name { get; set; }
        public List<ITextualDocumentation>? Children { get; set; }
    }
}