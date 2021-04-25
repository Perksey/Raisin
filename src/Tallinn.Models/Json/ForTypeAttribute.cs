using System;

namespace Tallinn.Models.Json
{
    internal class ForTypeAttribute : Attribute
    {
        public Type Type { get; }

        public ForTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}