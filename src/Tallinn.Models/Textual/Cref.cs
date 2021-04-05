using System.Collections.Generic;

namespace Tallinn.Models.Textual
{
    public class Cref
    {
        public bool IsExternal { get; set; }
        public string FullyQualifiedName { get; set; }
        public char Type { get; set; }
        public List<object> Specializations { get; set; } // for example parameter/return types
        public BaseXmlDocumentation? LocalDocumentation { get; set; } // null if IsExternal is true
    }

    public class Cref<T> : Cref where T : BaseXmlDocumentation
    {
        public new T? LocalDocumentation
        {
            get => base.LocalDocumentation as T;
            set => base.LocalDocumentation = value;
        }
    }
 }