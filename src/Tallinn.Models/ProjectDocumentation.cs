using System.Collections.Concurrent;

namespace Tallinn.Models
{
    public class ProjectDocumentation
    {
        public string Name { get; set; }
        public string AssemblyName { get; set; }
        public string PackageId { get; set; }
        public string Description { get; set; }
        public ConcurrentDictionary<string, NamespaceDocumentation> Namespaces { get; set; } = new();

        public RetrievalResult GetOrCreateNamespace(string name, out NamespaceDocumentation result)
        {
            var ret = RetrievalResult.Existed;
            result = Namespaces.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new NamespaceDocumentation();
            });
            
            return ret;
        }
    }
}