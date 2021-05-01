using System.Collections.Generic;
using Tallinn.Models.Json;

namespace Tallinn.Models.References
{
    public class Cref
    {
        public string? FullyQualifiedName { get; set; }
        public char Type { get; set; }
        public List<object> Specializations { get; set; } = new();// for example parameter/return types
        public LocalDocumentationHint? LocalDocumentation { get; set; } // if null, cref is external.
    }
 }