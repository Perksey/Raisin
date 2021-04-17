using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Raisin.Core;
using Raisin.PluginSystem;

[assembly: RaisinPlugin("Raisin.Plugins.TableOfContents")]

namespace Raisin.Plugins.TableOfContents
{
    public static class TableOfContentsPlugin
    {
        public static RaisinEngine WithTableOfContents(this RaisinEngine engine)
        {
            // other prerequisites
            var logger = engine.GetLoggerOrDefault(nameof(TableOfContentsPlugin));
            var matcher = engine.CreateGlobMatcher();
            matcher.AddInclude("toc.json");

            logger?.LogInformation("Loading table of contents models on disk...");

            // the goal of this section is to:
            // - load all ToC JSON files
            // - walk the tree in each of those files
            // - collect:
            //     - the file in which the ToC element is declared
            //     - the relative path to the file the ToC element refers to
            //     - the object representation of the ToC tree to which the ToC element belongs
            //     - the raw ToC element
            // this is because we need to have all that information represented in a nice way before "baking" the ToC
            // trees together, so we can just construct one big tree for all related ToCs.

            var loadedModels = 0;
            var notLoadedModels = 0;
            var rawTocModels = matcher.GetResultsInFullPath(engine.InputDirectory).Select(x =>
            {
                try
                {
                    // load all the ToC JSON file names we globbed.
                    return ((string, TableOfContentsElement?)?) (x,
                        JsonSerializer.Deserialize<TableOfContentsElement>(File.ReadAllText(x, Encoding.UTF8)));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, $"Couldn't parse table of contents file \"{x}\" as JSON: {ex.Message}");
                    notLoadedModels++;
                    return null;
                }
            }).Where(x => x?.Item2 != null).SelectMany(x =>
            {
                // get the file name of the ToC JSON file and the ToC model within it
                var (file, model) = x!.Value;

                // all paths in the model are relative to the directory in which the toc is contained.
                // get the path of the ToC in the input directory, and walk relative to that.
                var tocBasePath = Path.GetDirectoryName(Path.GetRelativePath(
                    engine.InputDirectory ?? throw new InvalidOperationException("No input directory provided."),
                    file))!;

                // indicate we have linking to do
                loadedModels++;

                // get all ToC entries
                var ret = Walk(model!, tocBasePath, model!).Select(y => (x.Value.Item1, y)).ToArray();

                // make all the Parent properties work
                CreateParentReferences(ret.Select(y => y.y.RootModel));

                // we're done!
                return ret;
            }).Select(x => (OriginalToCFile: x.Item1, Value: x.y));

            logger?.LogInformation($"{loadedModels} ToC models loaded, {notLoadedModels} failed to load.");

            // we can stop here if we don't have any models at all
            if (loadedModels == 0)
            {
                logger?.LogWarning("Models empty, no work to do.");
                return engine;
            }

            logger?.LogInformation("Baking models...");
            var tocModels = Bake(rawTocModels.ToArray(), engine, logger);


            return engine.WithRazorModelOverride((srcRel, @base) =>
            {
                var (dstRel, baseModel) = @base;
                // if we have a ToC model for the relative source or destination path, get it.
                if (!tocModels.TryGetValue(srcRel, out var val) && !tocModels.TryGetValue(dstRel, out val))
                {
                    return Task.FromResult(baseModel);
                }

                // deep clone the model we've found just in case the razor page decides to mess with it.
                val = Clone(val);

                // create the overriding model
                return Task.FromResult<object>
                (
                    new TableOfContentsModel
                    {
                        BaseModel = baseModel,
                        TocNode = val.Value,
                        TocRoot = val.Root
                    }
                );
            });
        }

        private static ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)> Bake
        (
            IReadOnlyList<(string OriginalToCFile, (string Rel, TableOfContentsElement RootModel, TableOfContentsElement
                Model) Value)> rawTocModels,
            RaisinEngine engine,
            ILogger? logger
        )
        {
            var wip = new Dictionary<string, (string OriginalToCFile, string Rel, TableOfContentsElement Root,
                TableOfContentsElement Value)>();
            // First pass removing duplicates
            foreach (var (tocFile, val) in rawTocModels)
            {
                var (srcRel, root, value) = val;
                var key = engine.UseCaseSensitivePaths ? srcRel : srcRel.ToUpper();
                if (!wip.TryAdd(key, (tocFile, srcRel, root, value)))
                {
                    continue;
                }

                if (wip.TryGetValue(key, out var existing))
                {
                    if (existing.Rel != srcRel && existing.Rel.Equals(srcRel, StringComparison.OrdinalIgnoreCase))
                    {
                        logger?.LogWarning($"Failed to add \"{srcRel}\" from \"{tocFile}\": duplicate key " +
                                           $"\"{existing.Rel}\" from \"{existing.OriginalToCFile}\" differing only " +
                                           "by case. Use WithCaseSensitivePaths to allow this.");
                    }
                    else
                    {
                        logger?.LogWarning($"Failed to add \"{srcRel}\" from \"{tocFile}\": duplicate key " +
                                           $"\"{existing.Rel}\" from \"{existing.OriginalToCFile}\"");
                    }
                }
                else
                {
                    logger?.LogWarning($"Failed to add \"{srcRel}\" from \"{tocFile}\": Unknown error.");
                }
            }

            // Second pass resolve fragmented models (that are linked together using ::path/to/inner/toc.json)
            var includedToCs = new Dictionary<string, string>();
            foreach (var tocFile in wip
                .Select(x => engine.UseCaseSensitivePaths ? x.Value.OriginalToCFile : x.Value.OriginalToCFile.ToUpper())
                .Distinct())
            {
                foreach (var (key, value) in wip)
                {
                    // get the key. if we're using case-sensitive paths, make it uppercase.
                    var ncsVal = engine.UseCaseSensitivePaths ? value.Value.Url : value.Value.Url?.ToUpper();
                    if (ncsVal is null)
                    {
                        continue;
                    }

                    // we need to check whether this a) is an include and b) whether the include refers to the current
                    // ToC file.
                    if (ncsVal.StartsWith("::") && ncsVal[2..].Equals(tocFile))
                    {
                        // looks like it is, get literally anything from the ToC tree that is from the file we want.
                        var referencedToCRoot = wip.FirstOrDefault(x =>
                            (engine.UseCaseSensitivePaths
                                ? x.Value.OriginalToCFile
                                : x.Value.OriginalToCFile.ToUpper()) == ncsVal[2..]);

                        // now, things would get incredibly weird if we let the same ToC be used twice, so let's not
                        // allow that.
                        if (includedToCs.TryGetValue(tocFile, out var val))
                        {
                            logger?.LogWarning("Detected inclusion of the same ToC more than once.");
                            logger?.LogWarning($"\"{tocFile}\" is included in \"{value.OriginalToCFile}\" and " +
                                               $"\"{val}\"");
                            logger?.LogWarning("Nuking it from the ToC tree...");
                            if (!wip.Remove(key))
                            {
                                logger?.LogWarning("Failed to nuke from ToC tree! The tree is now in an undefined " +
                                                   "state.");
                            }

                            continue;
                        }

                        if (!includedToCs.TryAdd(tocFile, value.OriginalToCFile))
                        {
                            logger?.LogWarning("Failed to add to ToC tree! The tree is now in an undefined state.");
                            continue;
                        }

                        // cool. so now we have:
                        // - key, which is the (probably uppercase) URI we got from walking the ToC trees
                        // - wip[key], which is the ToC element that contains the include.
                        // - referencedToCRoot, which is the root element of the ToC we're trying to include
                        var parent = wip[key].Value.Parent;
                        if (!(parent?.Children?.Remove(wip[key].Value) ?? true))
                        {
                            logger?.LogWarning("Failed to disown child.");
                        }

                        // create the parent-child relationship bonding the two ToC trees together

                        // set the included ToC's parent to the including ToC
                        referencedToCRoot.Value.Root.Parent = parent;
                        // add the included ToC as a child to the including ToC
                        parent?.Children?.Add(referencedToCRoot.Value.Root);
                        // replace the for the ToC inclusion with the included ToC
                        wip[key] = (tocFile, referencedToCRoot.Value.Rel, value.Root, referencedToCRoot.Value.Root);
                    }
                }
            }

            // Third pass getting it in the format the caller wants
            var ret = new ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)>();
            foreach (var (key, value) in wip)
            {
                ret.TryAdd(key, (value.Root, value.Value));
            }

            return ret;
        }

        private static IEnumerable<(string Rel, TableOfContentsElement RootModel, TableOfContentsElement Model)> Walk(
            TableOfContentsElement child, string tocBasePath, TableOfContentsElement root)
        {
            child.TocBasePath = tocBasePath;
            if (child.Url is not null)
            {
                yield return ($"{tocBasePath.PathFixup()}/{child.Url}".PathFixup(), root!, child);
            }
        }

        private static void CreateParentReferences
        (
            IEnumerable<TableOfContentsElement>? rootModels,
            TableOfContentsElement? parent = null
        )
        {
            foreach (var model in rootModels ?? Enumerable.Empty<TableOfContentsElement>())
            {
                model.Parent = parent;
                CreateParentReferences(model.Children, model);
            }
        }

        private static (TableOfContentsElement Root, TableOfContentsElement Value) Clone
        (
            (TableOfContentsElement Root, TableOfContentsElement Value) @in
        )
        {
            TableOfContentsElement? value = null;
            var root = CoreClone(@in.Root, @in.Value, ref value);
            if (value is null)
            {
                throw new ArgumentException("Given Value was not present within the Root");
            }

            return (root, value);
        }

        private static TableOfContentsElement CoreClone(TableOfContentsElement element,
            TableOfContentsElement lookForValue, ref TableOfContentsElement? value)
        {
            TableOfContentsElement? tempValue = null;
            var ret = new TableOfContentsElement
            {
                Name = element.Name,
                Url = element.Url,
                Children = element.Children?.Select(x =>
                {
                    var thisRet = CoreClone(x, lookForValue, ref tempValue);
                    if (x == lookForValue)
                    {
                        tempValue = thisRet;
                    }

                    return thisRet;
                }).ToList(),
                IsActive = element.IsActive,
                TocBasePath = element.TocBasePath
            };
            
            foreach (var elem in ret.Children ?? Enumerable.Empty<TableOfContentsElement>())
            {
                elem.Parent = ret;
            }

            value ??= tempValue;

            return ret;
        }
    }
}