using System;

namespace Tallinn.Models
{
    [Flags]
    public enum TypedCrefFlags
    {
        IsByRef = 1 << 0,
        IsIn = IsByRef | 1 << 1,
        IsOut = IsByRef | 1 << 2,
        IsPrimitive = 1 << 3
    }
}