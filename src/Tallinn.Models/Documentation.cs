using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Tallinn.Models
{
    public class Documentation
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, ProjectDocumentation> Projects { get; set; } = new();

        public static Documentation FromStream(Stream stream, bool leaveOpen = false)
        {
            using var gz = new GZipStream(stream, CompressionMode.Decompress, leaveOpen);
            using var sr = new StreamReader(gz);
            return JsonSerializer.Deserialize<Documentation>(sr.ReadToEnd());
        }

        public void ToStream(Stream stream, bool leaveOpen = false)
        {
            using var gz = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen);
            using var sw = new StreamWriter(gz);
            sw.Write(JsonSerializer.Serialize(this));
        }

        public RetrievalResult GetOrCreateProject(string name, out ProjectDocumentation result)
        {
            if (Projects.TryGetValue(name, out var project))
            {
                result = project;
                return RetrievalResult.Existed;
            }

            result = Projects[name] = new();
            return RetrievalResult.Created;
        }
    }
}