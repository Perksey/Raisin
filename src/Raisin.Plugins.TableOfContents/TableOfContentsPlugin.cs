using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Raisin.Core;
using Raisin.PluginSystem;

[assembly: RaisinPlugin("Raisin.Plugins.TableOfContents")]

namespace Raisin.Plugins.TableOfContents
{
    public static class TableOfContentsPlugin
    {
        private const string TocDict = nameof(Raisin) +
                                       "." +
                                       nameof(Plugins) +
                                       "." +
                                       nameof(TableOfContents) +
                                       "." +
                                       nameof(TableOfContentsPlugin) +
                                       "." +
                                       nameof(TocDict);

        private const string RebakeLock = nameof(Raisin) +
                                          "." +
                                          nameof(Plugins) +
                                          "." +
                                          nameof(TableOfContents) +
                                          "." +
                                          nameof(TableOfContentsPlugin) +
                                          "." +
                                          nameof(RebakeLock);

        public const string AddGeneratedTocCmd = nameof(Raisin) +
                                                 "." +
                                                 nameof(Plugins) +
                                                 "." +
                                                 nameof(TableOfContents) +
                                                 "." +
                                                 nameof(TableOfContentsPlugin) +
                                                 "." +
                                                 nameof(AddGeneratedTocCmd);

        public static RaisinEngine WithTableOfContents(this RaisinEngine engine)
        {
            // other prerequisites
            var logger = engine.GetLoggerOrDefault(nameof(TableOfContentsPlugin));
            var matcher = engine.CreateGlobMatcher();
            matcher.AddInclude("**/toc.json");

            logger?.LogInformation("Loading table of contents models on disk...");

            var loadedModels = 0;
            var notLoadedModels = 0;
            return engine.WithTableOfContents(matcher.GetResultsInFullPath(engine.InputDirectory).Select(x =>
            {
                var tmpNotLoadedModels = 0;
                var ret = LoadToC(x, ref tmpNotLoadedModels, logger);
                notLoadedModels += tmpNotLoadedModels;
                return ret;
            }).ToArray(), logger, ref loadedModels, ref notLoadedModels);
        }

        private static RaisinEngine WithTableOfContents(this RaisinEngine engine,
            IEnumerable<(string, TableOfContentsElement?)?> loadedToCs, ILogger? logger, ref int loadedModels,
            ref int notLoadedModels)
        {
            // the goal of this section is to:
            // - use all the ToC JSON files
            // - walk the tree in each of those files
            // - collect:
            //     - the file in which the ToC element is declared
            //     - the relative path to the file the ToC element refers to
            //     - the object representation of the ToC tree to which the ToC element belongs
            //     - the raw ToC element
            // this is because we need to have all that information represented in a nice way before "baking" the ToC
            // trees together, so we can just construct one big tree for all related ToCs.
            var rawTocModels = GetRawToCModels(loadedToCs, logger, ref loadedModels, engine);
            logger?.LogInformation($"{loadedModels} ToC models loaded, {notLoadedModels} failed to load.");

            ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)> tocModels;
            if (loadedModels == 0)
            {
                // we can stop here if we don't have any models at all
                logger?.LogWarning("Models empty, no work to do.");
                tocModels = new();
            }
            else
            {
                logger?.LogInformation("Baking models...");

                if (engine.InterPluginState.TryGetValue(RebakeLock, out var rebakeLock))
                {
                    // the ToC model may be updated while outputting 
                    lock (rebakeLock)
                    {
                        if (engine.InterPluginState.TryGetValue(TocDict, out var tocDictObj))
                        {
                            if (tocDictObj is not ConcurrentDictionary<string, (TableOfContentsElement Root,
                                TableOfContentsElement
                                Value)> tocDict)
                            {
                                logger?.LogWarning(
                                    "ToC dictionary inter-plugin state not as expected - another plugin probably " +
                                    "has state registered which conflicts with the ToC plugin's state. Discarding " +
                                    "state...");
                                return engine;
                            }

                            // we have to rebake the entire model, and to do that we need to get something that remotely
                            // resembles the original data we got. luckily, we can just use our handy Walk function to
                            // essentially replicate what we did earlier on.

                            tocModels = Bake
                            (
                                tocDict.Select(x => x.Value.Root)
                                    .Distinct()
                                    .SelectMany(x => Walk(x, x.TocBasePath, x.TocFile, x))
                                    .Select(x => (engine.GetSrcRel(x.RootModel.TocFile)!, x))
                                    .Concat(rawTocModels)
                                    .ToArray(),
                                engine,
                                logger
                            );

                            if (!engine.InterPluginState.TryUpdate(TocDict, tocModels, tocDict))
                            {
                                // this should be impossible because of the lock!
                                logger?.LogWarning("Couldn't update the state with the rebaked model due to an " +
                                                   "unknown error.");
                            }

                            return engine;
                        }
                    }
                }

                tocModels = Bake(rawTocModels.ToArray(), engine, logger);
            }

            void AddGeneratedTocCmdImpl(IEnumerable<(string, TableOfContentsElement?)?> x)
            {
                var thisLoadedModels = 0;
                var thisNotLoadedModels = 0;
                engine.WithTableOfContents(x, logger, ref thisLoadedModels, ref thisNotLoadedModels);
            }

            if (!engine.InterPluginState.TryAdd(TocDict, tocModels) || !engine.InterPluginState.TryAdd(
                    AddGeneratedTocCmd,
                    (Action<IEnumerable<(string, TableOfContentsElement?)?>>) AddGeneratedTocCmdImpl) ||
                !engine.InterPluginState.TryAdd(RebakeLock, new()))
            {
                logger?.LogWarning("Failed to register some inter-plugin state, interoperability between the ToC " +
                                   "plugin and other plugins may not work as expected.");
            }

            logger?.LogInformation("Plugin enabled.");

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

        private static
            IEnumerable<(string, (string Rel, TableOfContentsElement RootModel, TableOfContentsElement Model) x)>
            GetRawToCModels(IEnumerable<(string, TableOfContentsElement?)?> loadedToCs, ILogger? logger,
                ref int loadedModels, RaisinEngine engine)
        {
            var tmpLoadedModels = 0;
            var ret = loadedToCs
                .Where(x =>
                {
                    if (x is null)
                    {
                        return false;
                    }

                    if (x.Value.Item2 is not null)
                    {
                        return true;
                    }

                    logger?.LogWarning(
                        $"Loading of JSON ToC \"{x.Value.Item1}\" returned a null model, skipping...");
                    return false;
                }).SelectMany(x =>
                {
                    // get the file name of the ToC JSON file and the ToC model within it
                    var (file, model) = x!.Value;

                    // all paths in the model are relative to the directory in which the toc is contained.
                    // get the path of the ToC in the input directory, and walk relative to that.
                    var tocBasePath = engine.GetSrcRel(Path.GetDirectoryName(file)!)!;

                    // indicate we have linking to do
                    tmpLoadedModels++;

                    // get all ToC entries
                    var thisRet = Walk(model!, tocBasePath, file, model!).Select(y => (x.Value.Item1, y)).ToArray();

                    if (thisRet.Length == 0)
                    {
                        logger?.LogWarning($"Failed to walk! File: \"{file}\"");
                    }

                    // make all the Parent properties work
                    CreateParentReferences(thisRet.Select(y => y.y.RootModel));

                    // we're done!
                    return thisRet;
                }).Select(x => (OriginalToCFile: x.Item1, Value: x.y)).ToArray();
            loadedModels += tmpLoadedModels;
            return ret;
        }

        private static (string, TableOfContentsElement?)? LoadToC(string x, ref int notLoadedModels, ILogger? logger)
        {
            try
            {
                // load all the ToC JSON file names we globbed.
                return (x, JsonSerializer.Deserialize<TableOfContentsElement>(File.ReadAllText(x, Encoding.UTF8)));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Couldn't parse table of contents file \"{x}\" as JSON: {ex.Message}");
                notLoadedModels++;
                return null;
            }
        }

        private static ConcurrentDictionary<string, (TableOfContentsElement Root, TableOfContentsElement Value)> Bake
        (
            IEnumerable<(string OriginalToCFile, (string Rel, TableOfContentsElement RootModel, TableOfContentsElement
                Model) Value)> rawTocModels,
            RaisinEngine engine,
            ILogger? logger
        )
        {
            var wip = new Dictionary<string, (string OriginalToCFile, string Rel, TableOfContentsElement Root,
                TableOfContentsElement Value)>();

            // First pass removing duplicates
            void FirstPass(
                IEnumerable<(string OriginalToCFile, (string Rel, TableOfContentsElement RootModel,
                    TableOfContentsElement Model) Value)> thisRawTocModels)
            {
                foreach (var (tocFile, val) in thisRawTocModels)
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
                        else if (existing.OriginalToCFile == tocFile)
                        {
                            // do nothing
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
            }

            FirstPass(rawTocModels);

            // Second pass resolve fragmented models (that are linked together using ::path/to/inner/toc.json)
            var includedToCs = new Dictionary<string, string>(); // key: tocFile, value: file that includes the tocFile
            foreach (var (tocFile, actualTocFileName) in wip
                .Select(x => (engine.GetMaybeUpperSrcRel(x.Value.OriginalToCFile)!.PathFixup(), x.Value.OriginalToCFile))
                .DistinctBy(x=> x.Item1))
            {
                foreach (var (key, value) in wip)
                {
                    // get the key.
                    var rawNcsVal = value.Value.Url?.PathFixup();
                    // if we're using case-sensitive paths, make it uppercase.
                    var ncsVal = rawNcsVal is not null ? engine.MaybeUpper(rawNcsVal) : null;
                    if (ncsVal is null || rawNcsVal is null)
                    {
                        continue;
                    }

                    // we need to check whether this a) is an include and b) whether the include refers to the current
                    // ToC file.
                    if (!ncsVal.StartsWith("::"))
                    {
                        continue;
                    }
                    
                    var ncsF = $"{value.Value.TocBasePath}/{rawNcsVal[2..]}".PathFixup();
                    var muNcsF = engine.MaybeUpper(ncsF);
                    if (!muNcsF.Equals(tocFile))
                    {
                        continue;
                    }

                    // looks like it is, get literally anything from the ToC tree that is from the file we want.
                    KeyValuePair<string, (string OriginalToCFile, string Rel, TableOfContentsElement Root,
                        TableOfContentsElement Value)> referencedToCRoot;
                    for (var i = 0;; i++) // two-pass, because files that aren't named toc.json aren't loaded initially
                    {
                        referencedToCRoot = wip
                            .FirstOrDefault(x =>
                                engine.GetMaybeUpperSrcRel(x.Value.OriginalToCFile) == muNcsF);

                        // if this condition is true, this means that FirstOrDefault returned default (but we can't
                        // actually check that! grr...)
                        if (referencedToCRoot.Key is null! && referencedToCRoot.Value == default)
                        {
                            if (i == 0)
                            {
                                if (Path.GetFileName(tocFile) != engine.MaybeUpper("toc.json"))
                                {
                                    logger?.LogWarning(
                                        $"Included ToC file \"{ncsF}\" has a non-standard name and, as " +
                                        "a result, was not picked up on in the first pass. Generally, all " +
                                        "ToC files have the file name \"toc.json\".");
                                }

                                // first pass, let's try load the file seeing as it doesn't look like we have it in the
                                // ToC tree at the moment.
                                var whatever = 0;
                                var loaded = LoadToC(ncsF, ref whatever, logger);
                                FirstPass(GetRawToCModels(loaded.EnumerateOne(), logger, ref whatever, engine));
                                continue;
                            }

                            // second pass, we should have something from the included ToC file by now but apparently we
                            // don't, so give up.
                            logger?.LogWarning($"Couldn't find any ToC element from \"{ncsF}\" even " +
                                               "after forcibly including it into the ToC tree (is the included ToC " +
                                               "empty?). Nuking this inclusion from the ToC tree...");
                            if (!wip.Remove(key))
                            {
                                logger?.LogWarning("Failed to nuke from ToC tree! The tree is now in an undefined " +
                                                   "state.");
                            }
                        }

                        break;
                    }
                        
                    // if we haven't actually loaded the included ToC even after 2 passes, gloss over this as we
                    // should've removed the ToC inclusion in the second pass of the referencedToCRoot resolution.
                    if (referencedToCRoot.Key is null! && referencedToCRoot.Value == default)
                    {
                        continue;
                    }

                    // now, things would get incredibly weird if we let the same ToC be used twice, so let's not
                    // allow that.
                    if (includedToCs.TryGetValue(tocFile, out var val))
                    {
                        logger?.LogWarning("Detected inclusion of the same ToC more than once.");
                        logger?.LogWarning($"\"{actualTocFileName}\" is included in \"{value.OriginalToCFile}\" and " +
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
                    // remove the ToC inclusion from the ToC model now that the other models are in there
                    if (!wip.Remove(key))
                    {
                        logger?.LogWarning("Failed to add to ToC tree! The tree is now in an undefined state.");
                    }

                    // replace all elements referencing the included ToC as the root
                    foreach (var (referencingKey, referencingVal) in wip)
                    {
                        if (referencingVal.Root == referencedToCRoot.Value.Root)
                        {
                            wip[referencingKey] = (actualTocFileName, referencingVal.Rel, value.Root,
                                referencingVal.Value);
                        }
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
            TableOfContentsElement child, string tocBasePath, string tocFile, TableOfContentsElement root)
        {
            child.TocBasePath = tocBasePath;
            child.TocFile = tocFile;
            if (child.Url is not null)
            {
                yield return ($"{tocBasePath.PathFixup()}/{child.Url}".PathFixup(), root!, child);
            }

            // recurse for children
            foreach (var element in child.Children ?? Enumerable.Empty<TableOfContentsElement>())
            {
                foreach (var walked in Walk(element, tocBasePath, tocFile, root))
                {
                    yield return walked;
                }
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
                Metadata = element.Metadata?.ToDictionary(x => x.Key, x => x.Value),
                IsActive = false,
                TocBasePath = element.TocBasePath,
                TocFile = element.TocFile
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