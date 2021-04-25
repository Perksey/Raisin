using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tallinn.Models.References;
using Tallinn.Models.Textual;
using Tallinn.Models.Types;

namespace Tallinn.Models.Json
{
    public static class TallinnApiFile
    {
        private static JsonSerializerOptions Options => new()
        {
            WriteIndented = true, // indented is benign due to compression
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters =
            {
                new PolymorphicConverter<TypeDocumentation, TypeKind>(),
                new PolymorphicConverter<ITextualDocumentation, TextKind>(),
                new PolymorphicConverter<Cref, CrefKind>()
            }
        };
        
        public static Documentation? FromStream(Stream stream, bool leaveOpen = false)
        {
            using var gz = new GZipStream(stream, CompressionMode.Decompress, leaveOpen);
            using var sr = new StreamReader(gz);
            return JsonSerializer.Deserialize<Documentation>(sr.ReadToEnd(), Options);
        }

        public static void ToStream(this Documentation documentation, Stream stream, bool leaveOpen = false)
        {
            using var gz = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen);
            using var sw = new StreamWriter(gz);
            sw.Write(JsonSerializer.Serialize(documentation, Options));
        }
    }
}