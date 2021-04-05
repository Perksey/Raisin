using System.Collections.Generic;

namespace Tallinn.Models
{
    public class NamespaceDocumentation
    {
        public string Namespace { get; set; }
        public List<TypeDocumentation> Types { get; set; }
    }
}