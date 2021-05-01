using System.Collections.Generic;
using Tallinn.Models.References;

namespace Tallinn.Models.Types
{
    public sealed class InterfaceDocumentation : MemberedTypeDocumentation
    {
        public List<Cref> Implements { get; set; } = new(); // crefs to the interfaces this interface implements
    }
}