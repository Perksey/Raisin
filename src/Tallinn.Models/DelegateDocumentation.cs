using System.Collections.Immutable;

namespace Tallinn.Models
{
    public class DelegateDocumentation : TypeDocumentation
    {
        public ImmutableDictionary<string, TypeReferenceDocumentation> Parameters { get; set; }
        public TypeReferenceDocumentation Returns { get; set; }
    }
}