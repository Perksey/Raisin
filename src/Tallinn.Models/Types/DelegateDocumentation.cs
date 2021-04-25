using System.Collections.Immutable;
using Tallinn.Models.References;

namespace Tallinn.Models.Types
{
    public sealed class DelegateDocumentation : TypeDocumentation
    {
        public ImmutableDictionary<string, TypeReferenceDocumentation> Parameters { get; set; }
        public TypeReferenceDocumentation Returns { get; set; }
    }
}