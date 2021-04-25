using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models.References
{
    public sealed class TypeParameterDocumentation
    {
        public List<string> Constraints { get; set; }
        public ITextualDocumentation? Documentation { get; set; }
    }
}