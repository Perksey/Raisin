using Tallinn.Models.References;

namespace Tallinn.Models.Json
{
    internal enum CrefKind
    {
        [ForType(typeof(Cref))] Cref,
        [ForType(typeof(TypedCref))] TypedCref
    }
}