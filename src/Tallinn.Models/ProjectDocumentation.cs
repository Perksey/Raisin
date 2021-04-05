using System.Collections.Generic;

namespace Tallinn.Models
{
    public class ProjectDocumentation
    {
        public string Description { get; set; }
        public List<NamespaceDocumentation> Namespaces { get; set; }
    }
}