using System.Collections.Generic;
using Tallinn.Models.Textual;

namespace Tallinn.Models
{
    public class ClassDocumentation : MemberedTypeDocumentation
    {
        public List<Cref> Heirarchy { get; set; }
        public List<Cref> Implements { get; set; }
    }
}