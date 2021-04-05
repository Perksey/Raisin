using System.Collections.Generic;

namespace Tallinn.Models
{
    public abstract class MemberedTypeDocumentation : TypeDocumentation
    {
        public List<MemberDocumentation> Members { get; set; }
        public List<MethodDocumentation> Methods { get; set; }
    }
}