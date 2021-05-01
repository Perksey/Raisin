using System.Collections.Concurrent;
using System.Collections.Generic;
using Tallinn.Models.Types;

namespace Tallinn.Models
{
    public sealed class NamespaceDocumentation
    {
        public string? Namespace { get; set; }
        public ConcurrentDictionary<string, TypeDocumentation> Types { get; set; } = new();

        public RetrievalResult GetOrCreateClass(string name, out ClassDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new ClassDocumentation {TypeName = name};
            }) as ClassDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrCreateStruct(string name, out StructDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new StructDocumentation {TypeName = name};
            }) as StructDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrCreateRecord(string name, out RecordDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new RecordDocumentation {TypeName = name};
            }) as RecordDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrCreateDelegate(string name, out DelegateDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new DelegateDocumentation {TypeName = name};
            }) as DelegateDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrCreateInterface(string name, out InterfaceDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Types.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new InterfaceDocumentation {TypeName = name};
            }) as InterfaceDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }
    }
}