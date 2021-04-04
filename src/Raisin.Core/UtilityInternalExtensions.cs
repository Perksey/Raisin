using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Raisin.Plugins.TableOfContents")]

namespace Raisin.Core
{
    internal static class UtilityInternalExtensions
    {
        public static IEnumerable<T> EnumerateOne<T>(this T val)
        {
            yield return val;
        }
        
        public static string PathFixup(this string x) => x.Replace("\\", "/").Trim('/');
    }
}