using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public class StructDocumentation : MemberedTypeDocumentation
    {
        public List<Cref<InterfaceDocumentation>> Implements { get; set; }
    }
}