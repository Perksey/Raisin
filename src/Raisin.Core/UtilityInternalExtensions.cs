using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Raisin.Plugins.TableOfContents")]
[assembly: InternalsVisibleTo("Raisin.Plugins.Markdown")]

namespace Raisin.Core
{
    internal static class UtilityInternalExtensions
    {
        public static IEnumerable<T> EnumerateOne<T>(this T val)
        {
            yield return val;
        }
        
        public static string PathFixup(this string x) => x.Replace("\\", "/").Trim('/');

        public static string CreateDirectoryIfNeeded(this string x)
        {
            if (!Directory.Exists(x))
            {
                CreateDirectoryIfNeeded(Path.GetDirectoryName(x)!);
                Directory.CreateDirectory(x);
            }

            return x;
        }

        public static string CreateFileDirectoryIfNeeded(this string x)
        {
            CreateDirectoryIfNeeded(Path.GetDirectoryName(x)!);
            return x;
        }
    }
}