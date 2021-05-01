using System.Collections.Generic;
using Tallinn.Models.References;

namespace Tallinn.Models.Types
{
    public sealed class ClassDocumentation : MemberedTypeDocumentation
    {
        public List<Cref> Heirarchy { get; set; } = new();
        public List<Cref> Implements { get; set; } = new();
    }
}