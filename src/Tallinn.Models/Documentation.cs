using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Tallinn.Models
{
    public sealed class Documentation
    {
        public string? Name { get; set; }
        public ConcurrentDictionary<string, ProjectDocumentation> Projects { get; set; } = new();
        public RetrievalResult GetOrCreateProject(string name, out ProjectDocumentation result)
        {
            var ret = RetrievalResult.Existed;
            result = Projects.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new() {Name = name};
            });
            
            return ret;
        }
    }
}