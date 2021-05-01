using System.Collections.Concurrent;
using Tallinn.Models.Members;

namespace Tallinn.Models.Types
{
    public abstract class MemberedTypeDocumentation : TypeDocumentation
    {
        public ConcurrentDictionary<string, MemberDocumentation> Members { get; set; } = new();

        public RetrievalResult GetOrAddField(string name, out FieldDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Members.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new FieldDocumentation {Name = name};
            }) as FieldDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrAddMethod(string name, out MethodDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Members.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new MethodDocumentation {Name = name};
            }) as MethodDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }

        public RetrievalResult GetOrAddProperty(string name, out PropertyDocumentation? result)
        {
            var ret = RetrievalResult.Existed;
            result = Members.GetOrAdd(name, _ =>
            {
                ret = RetrievalResult.Created;
                return new PropertyDocumentation {Name = name};
            }) as PropertyDocumentation;
            return result is null ? RetrievalResult.ErrorExistedTypeMismatch : ret;
        }
    }
}