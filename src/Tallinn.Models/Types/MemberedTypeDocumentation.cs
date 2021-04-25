using System.Collections.Generic;
using Tallinn.Models.Members;

namespace Tallinn.Models.Types
{
    public abstract class MemberedTypeDocumentation : TypeDocumentation
    {
        public List<MemberDocumentation> Members { get; set; }
    }
}