using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public class InterfaceDocumentation : MemberedTypeDocumentation
    {
        public List<Cref<InterfaceDocumentation>> Implements { get; set; }
    }
}