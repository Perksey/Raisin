using Tallinn.Models.Types;

namespace Tallinn.Models.Json
{
    internal enum TypeKind
    {
        [ForType(typeof(ClassDocumentation))] Class,
        [ForType(typeof(StructDocumentation))] Struct,
        [ForType(typeof(RecordDocumentation))] Record,
        [ForType(typeof(DelegateDocumentation))] Delegate,
        [ForType(typeof(InterfaceDocumentation))] Interface
    }
}