using System.Collections.Concurrent;
using System.Collections.Immutable;
using Tallinn.Models.References;

namespace Tallinn.Models.Types
{
    public sealed class DelegateDocumentation : TypeDocumentation
    {
        public ConcurrentDictionary<string, TypeReferenceDocumentation> Parameters { get; set; } = new();
        public TypeReferenceDocumentation? Returns { get; set; }
    }
}