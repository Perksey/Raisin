using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tallinn.Models
{
    public class NamespaceDocumentation
    {
        public string Namespace { get; set; }
        public ConcurrentDictionary<string, TypeDocumentation> Types { get; set; }
        public RetrievalResult GetOrCreateType(string name, out TypeDocumentation result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new TypeDocumentation();
            });
            
            return ret;
        }
    }
}