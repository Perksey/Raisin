using System.Collections.Concurrent;
using System.Collections.Generic;
using Tallinn.Models.Types;

namespace Tallinn.Models
{
    public sealed class NamespaceDocumentation
    {
        public string Namespace { get; set; }
        public ConcurrentDictionary<string, TypeDocumentation> Types { get; set; }
    }
}