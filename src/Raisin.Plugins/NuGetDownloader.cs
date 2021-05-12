using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using INuGetLogger = NuGet.Common.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace Raisin.PluginSystem
{
    public static class NuGetDownloader
    {
        private static readonly SourceCacheContext _cache = new();

        public static async IAsyncEnumerable<Assembly> DownloadAsync(string id, string? version, string packagesPath,
            string[] feeds, ILogger? logger)
        {
            var actionLogger = new LoggerAdapter(logger);
            var frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttributes(true)
                .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                .Select(x => x.FrameworkName)
                .FirstOrDefault();
            NuGetFramework framework = frameworkName == null
                ? NuGetFramework.AnyFramework
                : NuGetFramework.ParseFrameworkName(frameworkName, new DefaultFrameworkNameProvider());

            using var cache = new SourceCacheContext();
            await CreateEmptyNugetConfig(packagesPath, feeds);

            var settings =
                Settings.LoadImmutableSettingsGivenConfigPaths(new[] {Path.Combine(packagesPath, "empty.config")},
                    new());

            var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            var repositoryProvider = new SourceRepositoryProvider(new PackageSourceProvider(settings),
                new Repository.ProviderFactory().GetCoreV3());

            var repository = repositoryProvider.GetRepositories().FirstOrDefault() ??
                             throw new NuGetResolverException("Unable to resolve a repository.");
            var packageMetadataResource =
                await repository.GetResourceAsync<PackageMetadataResource>(CancellationToken.None);
            var searchMetadata = await packageMetadataResource.GetMetadataAsync(
                id,
                includePrerelease: false,
                includeUnlisted: false,
                cache,
                actionLogger,
                CancellationToken.None);
            searchMetadata = searchMetadata.ToArray();

            if (!searchMetadata.Any())
            {
                throw new NuGetResolverException($"Unable to resolve nuget package with id {id}");
            }

            var latest = searchMetadata.OrderByDescending(a => a.Identity.Version).FirstOrDefault();

            if (latest is null)
            {
                throw new NuGetResolverException($"Unable to resolve nuget package with id {id}");
            }

            var packageId = latest.Identity;
            var dependencyResource = await repository.GetResourceAsync<DependencyInfoResource>();

            await GetPackageDependencies(
                packageId,
                framework,
                cache,
                repository,
                dependencyResource,
                availablePackages, actionLogger);

            var resolverContext = new PackageResolverContext(
                DependencyBehavior.Lowest,
                new[] {id},
                Enumerable.Empty<string>(),
                Enumerable.Empty<PackageReference>(),
                version is null
                    ? Enumerable.Empty<PackageIdentity>()
                    : new[] {new PackageIdentity(id, NuGetVersion.Parse(version))},
                availablePackages,
                new[] {repository.PackageSource},
                actionLogger);

            var resolver = new PackageResolver();
            var toInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                .Select(a => availablePackages.Single(b => PackageIdentityComparer.Default.Equals(b, a)));

            var pathResolver = new PackagePathResolver(packagesPath);
            var extractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.None,
                ClientPolicyContext.GetClientPolicy(settings, actionLogger),
                actionLogger);
            var libraries = new List<string>();
            var frameworkReducer = new FrameworkReducer();
            var downloadResource = await repository.GetResourceAsync<DownloadResource>(CancellationToken.None);
            foreach (var package in toInstall)
            {
                libraries.AddRange(await Install(downloadResource, package, pathResolver, extractionContext,
                    frameworkReducer, framework, packagesPath, actionLogger));
            }

            foreach (var library in libraries)
            {
                yield return AssemblyLoadContext.Default.LoadFromAssemblyPath(library);
            }
            
            logger?.LogInformation($"NuGet package \"{id}\" has been loaded.");
        }

        private static async Task CreateEmptyNugetConfig(string packagesPath, string[] feeds)
        {
            var filename = "empty.config";
            var fullPath = Path.Combine(packagesPath, filename);
            Directory.CreateDirectory(packagesPath);

            if (!File.Exists(fullPath))
            {
                await File.WriteAllTextAsync(fullPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <config>
    <add key=""repositoryPath"" value=""{packagesPath}"" />
    <add key=""globalPackagesFolder"" value=""{packagesPath}"" />
  </config>
  <packageSources>
    {string.Join("\n    ", feeds.Select((x, i) => $@"<add key=""Feed{i}"" value=""{x}"" />"))}
  </packageSources>
</configuration>");
            }
        }

        private static async Task<IEnumerable<string>> Install(
            DownloadResource downloadResource,
            PackageIdentity package,
            PackagePathResolver pathResolver,
            PackageExtractionContext extractionContext,
            FrameworkReducer reducer,
            NuGetFramework framework,
            string packagesPath,
            INuGetLogger logger)
        {
            var packageResult = await downloadResource.GetDownloadResourceResultAsync(
                package,
                new(_cache),
                packagesPath,
                logger,
                CancellationToken.None);
            await PackageExtractor.ExtractPackageAsync(
                packageResult.PackageSource,
                packageResult.PackageStream,
                pathResolver, extractionContext,
                CancellationToken.None);

            var libItems = await packageResult.PackageReader.GetLibItemsAsync(CancellationToken.None);
            libItems = libItems.ToArray();
            var nearest = reducer.GetNearest(framework, libItems.Select(a => a.TargetFramework));
            var selected = libItems.Where(a => a.TargetFramework.Equals(nearest)).SelectMany(a => a.Items);
            return selected.Where(a => Path.GetExtension(a) == ".dll")
                .Select(a => Path.Combine(pathResolver.GetInstalledPath(package), a));
        }

        private static async Task GetPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            SourceRepository repository,
            DependencyInfoResource dependencyInfoResource,
            ISet<SourcePackageDependencyInfo> availablePackages,
            INuGetLogger logger)
        {
            if (availablePackages.Contains(package)) return;

            var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                package,
                framework,
                cacheContext,
                logger,
                CancellationToken.None);

            if (dependencyInfo == null)
            {
                return;
            }

            availablePackages.Add(dependencyInfo);
            foreach (var dependency in dependencyInfo.Dependencies)
            {
                await GetPackageDependencies(
                    new PackageIdentity(
                        dependency.Id,
                        dependency.VersionRange.MinVersion),
                    framework,
                    cacheContext,
                    repository,
                    dependencyInfoResource,
                    availablePackages, logger);
            }
        }

        class LoggerAdapter : INuGetLogger
        {
            private ILogger? _logger;
            public LoggerAdapter(ILogger? logger) => _logger = logger;
            public void LogDebug(string data) => _logger?.LogDebug(data);
            public void LogVerbose(string data) => _logger?.LogTrace(data);
            public void LogInformation(string data) => _logger?.LogInformation(data);
            public void LogMinimal(string data) => _logger?.Log(Microsoft.Extensions.Logging.LogLevel.None, data);
            public void LogWarning(string data) => _logger?.LogWarning(data);
            public void LogError(string data) => _logger?.LogError(data);
            public void LogInformationSummary(string data) => _logger?.LogInformation(data);

            public void Log(LogLevel level, string data)
            {
                switch (level)
                {
                    case LogLevel.Debug:
                    {
                        LogDebug(data);
                        break;
                    }
                    case LogLevel.Verbose:
                    {
                        LogVerbose(data);
                        break;
                    }
                    case LogLevel.Information:
                    {
                        LogInformation(data);
                        break;
                    }
                    case LogLevel.Minimal:
                    {
                        LogMinimal(data);
                        break;
                    }
                    case LogLevel.Warning:
                    {
                        LogWarning(data);
                        break;
                    }
                    case LogLevel.Error:
                    {
                        LogError(data);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(level), level, null);
                }
            }

            public Task LogAsync(LogLevel level, string data)
            {
                Log(level, data);
                return Task.CompletedTask;
            }

            public void Log(ILogMessage message) => Log(message.Level, message.Message);
            public Task LogAsync(ILogMessage message) => LogAsync(message.Level, message.Message);
        }
    }
}