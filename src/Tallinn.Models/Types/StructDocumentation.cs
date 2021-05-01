using System.Collections.Generic;
using Tallinn.Models.References;

namespace Tallinn.Models.Types
{
    public sealed class StructDocumentation : MemberedTypeDocumentation
    {
        public List<Cref> Implements { get; set; } = new(); // crefs to the interfaces this struct implements
    }
}