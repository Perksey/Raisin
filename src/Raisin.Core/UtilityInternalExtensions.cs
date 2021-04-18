using System;
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

        public static string PathFixup(this string x)
        {
            var ret = x.Replace("\\", "/").Trim('/');
            return ret.StartsWith("./") ? ret[2..] : ret;
        }

        public static string CreateDirectoryIfNeeded(this string x)
        {
            if (!Directory.Exists(x))
            {
                CreateDirectoryIfNeeded(Path.GetDirectoryName(x)!); // create all parent directories as well
                Directory.CreateDirectory(x); // create the directory
            }

            return x;
        }

        public static string CreateFileDirectoryIfNeeded(this string x)
        {
            CreateDirectoryIfNeeded(Path.GetDirectoryName(x)!);
            return x;
        }

        public static string? GetSrcRel(this RaisinEngine engine, string path) => 
            Path.GetRelativePath(
                engine.InputDirectory ??
                throw new InvalidOperationException("No input directory provided."), path).PathFixup();

        public static string GetSrcAbs(this RaisinEngine engine, string path)
            => Path.Combine(engine.InputDirectory!, path);

        public static string? GetMaybeUpperSrcRel(this RaisinEngine engine, string path)
            => engine.GetSrcRel(engine.UseCaseSensitivePaths ? path : path.ToUpper());

        public static string MaybeUpper(this RaisinEngine engine, string path)
            => engine.UseCaseSensitivePaths ? path : path.ToUpper();
    }
}