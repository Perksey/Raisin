using Tallinn.Models.Members;

namespace Tallinn.Models.Json
{
    internal enum MemberKind
    {
        [ForType(typeof(FieldDocumentation))] Field,
        [ForType(typeof(MethodDocumentation))] Method,
        [ForType(typeof(PropertyDocumentation))] Property
    }
}