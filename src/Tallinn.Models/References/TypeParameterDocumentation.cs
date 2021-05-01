using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models.References
{
    public sealed class TypeParameterDocumentation
    {
        public int Index { get; set; }
        public List<string> Constraints { get; set; } = new();
        public ITextualDocumentation? Documentation { get; set; }
    }
}