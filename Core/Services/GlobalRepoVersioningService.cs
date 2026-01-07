using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml;
using AnubisWorks.Tools.Versioner.Entity;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Services.Versioning;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    public interface IGlobalRepoVersioningService
    {
        void VersionGlobalRepository(string repositoryRoot, string buildLabel, VersionedSetModel versionedSetModel, bool storeVersionFile, string prereleaseSuffix = null, List<string>? allowedTypes = null);
    }

    public class GlobalRepoVersioningService : IGlobalRepoVersioningService
    {
        private readonly ILogger _log;
        private readonly IFileOperations _fileOperations;
        private readonly IArtifactDiscoveryService _artifactDiscoveryService;
        private readonly VersionManager _versionManager;
        private readonly VersioningBaseConfiguration _defaultConfig;
        private readonly IPythonVersioningService _pythonVersioningService;
        private readonly IGoVersioningService _goVersioningService;
        private readonly IRustVersioningService _rustVersioningService;
        private readonly IJavaVersioningService _javaVersioningService;
        private readonly IYamlVersioningService _yamlVersioningService;

        public GlobalRepoVersioningService(ILogger log, IFileOperations fileOperations, IArtifactDiscoveryService? artifactDiscoveryService = null)
        {
            _log = log;
            _fileOperations = fileOperations;
            _artifactDiscoveryService = artifactDiscoveryService ?? new ArtifactDiscoveryService(fileOperations, log);
            _versionManager = new VersionManager(log);
            
            // Initialize versioning services for new formats
            _pythonVersioningService = new Versioning.PythonVersioningService(log, fileOperations);
            _goVersioningService = new Versioning.GoVersioningService(log, fileOperations);
            _rustVersioningService = new Versioning.RustVersioningService(log, fileOperations);
            _javaVersioningService = new Versioning.JavaVersioningService(log, fileOperations);
            _yamlVersioningService = new Versioning.YamlVersioningService(log, fileOperations);
            
            // Default configuration for versioning
            _defaultConfig = new VersioningBaseConfiguration
            {
                AssemblyVersionSet = true,
                AssemblyFileVersionSet = true,
                AssemblyInfoVersionSet = true,
                AssemblyVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyFileVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyInfoVersionFormat = "*.{0:00}{1:00}.{5}.{2}+{3}"
            };
        }

        public void VersionGlobalRepository(string repositoryRoot, string buildLabel, VersionedSetModel versionedSetModel, bool storeVersionFile, string prereleaseSuffix = null, List<string>? allowedTypes = null)
        {
            _log.Information("Starting global repository versioning in: {root}", repositoryRoot);
            
            // Use ArtifactDiscoveryService to discover all artifacts (respects VersionItems filter)
            // In monorepo mode, exclude .props files from discovery (we only handle Directory.Build.props)
            var discoveryAllowedTypes = allowedTypes;
            if (discoveryAllowedTypes == null || discoveryAllowedTypes.Count == 0)
            {
                // Exclude props from discovery in monorepo mode
                discoveryAllowedTypes = new List<string> { "dotnet", "nuget", "npm", "docker", "python", "go", "rust", "java", "helm", "yaml" };
            }
            else if (!discoveryAllowedTypes.Contains("props", StringComparer.OrdinalIgnoreCase))
            {
                // Props not in allowed types, keep as is
            }
            else
            {
                // Remove props from allowed types in monorepo mode
                discoveryAllowedTypes = discoveryAllowedTypes.Where(t => !t.Equals("props", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            var discoveryResult = _artifactDiscoveryService.DiscoverArtifacts(repositoryRoot, recursive: true, allowedTypes: discoveryAllowedTypes);
            
            _log.Information("Found artifacts to version: {total} total (dotnet: {dotnet}, npm: {npm}, docker: {docker}, python: {python}, go: {go}, rust: {rust}, java: {java}, helm: {helm}, yaml: {yaml})",
                discoveryResult.TotalArtifacts,
                discoveryResult.DotNetProjects.Count,
                discoveryResult.NpmPackages.Count,
                discoveryResult.DockerArtifacts.Count,
                discoveryResult.PythonProjects.Count,
                discoveryResult.GoModules.Count,
                discoveryResult.RustProjects.Count,
                discoveryResult.JavaProjects.Count,
                discoveryResult.HelmCharts.Count,
                discoveryResult.YamlConfigs.Count);
            
            int versionedCount = 0;
            var versionString = string.IsNullOrEmpty(prereleaseSuffix) 
                ? versionedSetModel.AssemblyVersion 
                : $"{versionedSetModel.AssemblyVersion}-{prereleaseSuffix}";
            var gitHash = versionedSetModel.AssemblyInfoVersion.Contains("+") 
                ? versionedSetModel.AssemblyInfoVersion.Split('+').LastOrDefault() ?? ""
                : "";
            
            // In monorepo mode: check for Directory.Build.props in repository root
            // If it exists, modify it; otherwise, modify .csproj files
            var directoryBuildPropsPath = Path.Combine(repositoryRoot, "Directory.Build.props");
            bool hasDirectoryBuildProps = _fileOperations.FileExists(directoryBuildPropsPath);
            
            if (hasDirectoryBuildProps)
            {
                // Modify existing Directory.Build.props
                _log.Information("Found Directory.Build.props in repository root, modifying it with version information");
                VersionDotNetFile(directoryBuildPropsPath, versionedSetModel, prereleaseSuffix, ProjectType.Props);
                versionedCount++;
                _log.Information("Modified Directory.Build.props file in repository root: {file}", directoryBuildPropsPath);
            }
            else
            {
                // No Directory.Build.props - modify .csproj files directly
                _log.Information("Directory.Build.props not found in repository root, modifying .csproj files directly");
                foreach (var artifact in discoveryResult.DotNetProjects)
                {
                    try
                    {
                        VersionDotNetFile(artifact.FilePath, versionedSetModel, prereleaseSuffix, ProjectType.Sdk);
                        versionedCount++;
                        _log.Debug("Versioned .NET project: {file}", artifact.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _log.Warning(ex, "Failed to version .NET project: {file}", artifact.FilePath);
                    }
                }
            }
            
            // Skip other .props files in monorepo mode (we only handle Directory.Build.props)
            
            // Version NuGet packages
            foreach (var artifact in discoveryResult.NuGetPackages)
            {
                try
                {
                    VersionNuGetFile(artifact.FilePath, versionedSetModel.AssemblyVersion, prereleaseSuffix);
                    versionedCount++;
                    _log.Debug("Versioned NuGet package: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version NuGet package: {file}", artifact.FilePath);
                }
            }
            
            // Version NPM packages
            foreach (var artifact in discoveryResult.NpmPackages)
            {
                try
                {
                    VersionNpmFile(artifact.FilePath, versionedSetModel.AssemblyVersion, prereleaseSuffix);
                    versionedCount++;
                    _log.Debug("Versioned NPM package: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version NPM package: {file}", artifact.FilePath);
                }
            }
            
            // Version Docker artifacts
            foreach (var artifact in discoveryResult.DockerArtifacts)
            {
                try
                {
                    VersionDockerFile(artifact.FilePath, versionedSetModel.AssemblyVersion, prereleaseSuffix);
                    versionedCount++;
                    _log.Debug("Versioned Docker artifact: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Docker artifact: {file}", artifact.FilePath);
                }
            }
            
            // Version Python projects (Phase 2)
            foreach (var artifact in discoveryResult.PythonProjects)
            {
                try
                {
                    _pythonVersioningService.VersionPythonProject(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned Python project: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Python project: {file}", artifact.FilePath);
                }
            }
            
            // Version Go modules (Phase 2)
            foreach (var artifact in discoveryResult.GoModules)
            {
                try
                {
                    _goVersioningService.VersionGoModule(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned Go module: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Go module: {file}", artifact.FilePath);
                }
            }
            
            // Version Rust projects (Phase 2)
            foreach (var artifact in discoveryResult.RustProjects)
            {
                try
                {
                    _rustVersioningService.VersionRustProject(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned Rust project: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Rust project: {file}", artifact.FilePath);
                }
            }
            
            // Version Java projects (Phase 2)
            foreach (var artifact in discoveryResult.JavaProjects)
            {
                try
                {
                    _javaVersioningService.VersionJavaProject(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned Java project: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Java project: {file}", artifact.FilePath);
                }
            }
            
            // Version Helm charts (Phase 2)
            foreach (var artifact in discoveryResult.HelmCharts)
            {
                try
                {
                    _yamlVersioningService.VersionYamlFile(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned Helm chart: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version Helm chart: {file}", artifact.FilePath);
                }
            }
            
            // Version YAML configs (Phase 2)
            foreach (var artifact in discoveryResult.YamlConfigs)
            {
                try
                {
                    _yamlVersioningService.VersionYamlFile(artifact.FilePath, versionString, buildLabel, gitHash);
                    versionedCount++;
                    _log.Debug("Versioned YAML config: {file}", artifact.FilePath);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to version YAML config: {file}", artifact.FilePath);
                }
            }
            
            // Store version.txt if requested (respect storeVersionFile parameter)
            if (storeVersionFile && versionedCount > 0)
            {
                var versionText = string.IsNullOrEmpty(prereleaseSuffix) 
                    ? versionedSetModel.AssemblyVersion 
                    : $"{versionedSetModel.AssemblyVersion}-{prereleaseSuffix}";
                
                var versionFilePath = Path.Combine(repositoryRoot, "version.txt");
                _fileOperations.WriteFileContent(versionFilePath, versionText);
                _log.Information("Stored version file: {file} with version: {version}", versionFilePath, versionText);
            }
            
            _log.Information("Global repository versioning completed. Versioned {count} files.", versionedCount);
        }
        
        private void VersionDotNetFile(string filePath, VersionedSetModel versionedSetModel, string prereleaseSuffix, ProjectType projectType)
        {
            if (!_fileOperations.FileExists(filePath))
            {
                _log.Warning("File not found: {file}", filePath);
                return;
            }
            
            var content = _fileOperations.ReadFileContent(filePath);
            var project = XDocument.Parse(content);
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            
            string description = $"Version: {versionedSetModel.BuildLabel}";
            
            string assemblyInfoFilePath = string.Empty;
            _versionManager.SetVersion(
                ref saveprops,
                ref savecsproj,
                ref saveNuSpec,
                project,
                _defaultConfig,
                projectType,
                description,
                versionedSetModel.AssemblyFileVersion,
                versionedSetModel.AssemblyVersion,
                versionedSetModel.AssemblyInfoVersion,
                filePath,
                assemblyInfoFilePath,
                prereleaseSuffix);
            
            if (savecsproj && projectType == ProjectType.Sdk)
            {
                // Clean up empty elements
                foreach (var child in project.Descendants().Reverse())
                {
                    if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) 
                        child.Remove();
                }
                
                var projectContent = project.ToString(SaveOptions.None)
                    .Replace("-&gt;", "->")
                    .Replace("<ProjectGuid xmlns=\"\">", "<ProjectGuid>");
                _fileOperations.WriteFileContent(filePath, projectContent);
            }
            else if (saveprops && projectType == ProjectType.Props)
            {
                // Clean up empty elements
                foreach (var child in project.Descendants().Reverse())
                {
                    if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) 
                        child.Remove();
                }
                
                var projectContent = project.ToString(SaveOptions.None)
                    .Replace("-&gt;", "->");
                _fileOperations.WriteFileContent(filePath, projectContent);
            }
        }
        
        private void VersionNpmFile(string filePath, string version, string prereleaseSuffix)
        {
            if (!_fileOperations.FileExists(filePath))
            {
                _log.Warning("File not found: {file}", filePath);
                return;
            }
            
            // Read package.json content using IFileOperations (platform independent, works with mocks)
            var content = _fileOperations.ReadFileContent(filePath);
            var jObject = JObject.Parse(content);
            
            // Set version with optional prerelease suffix
            if (string.IsNullOrEmpty(prereleaseSuffix))
            {
                jObject["version"] = version;
            }
            else
            {
                jObject["version"] = version + "-" + prereleaseSuffix;
            }
            
            // Write back using IFileOperations
            _fileOperations.WriteFileContent(filePath, jObject.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        
        private void VersionDockerFile(string filePath, string version, string prereleaseSuffix)
        {
            if (!_fileOperations.FileExists(filePath))
            {
                _log.Warning("File not found: {file}", filePath);
                return;
            }
            
            var content = _fileOperations.ReadFileContent(filePath);
            var versionString = string.IsNullOrEmpty(prereleaseSuffix) ? version : $"{version}-{prereleaseSuffix}";
            
            // Add or update ARG VERSION at the beginning (after FROM if present)
            bool hasArgVersion = Regex.IsMatch(content, @"^\s*ARG\s+VERSION", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            bool hasVersionComment = content.Contains($"# Version: {versionString}") || 
                                   Regex.IsMatch(content, @"#\s*Version:\s*.*", RegexOptions.IgnoreCase);
            
            if (!hasArgVersion)
            {
                // Find the first FROM statement
                var fromMatch = Regex.Match(content, @"^(FROM\s+[^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (fromMatch.Success)
                {
                    // Insert ARG VERSION after the first FROM
                    var insertPosition = fromMatch.Index + fromMatch.Length;
                    content = content.Insert(insertPosition, $"\nARG VERSION={versionString}\n");
                }
                else
                {
                    // If no FROM, add at the beginning
                    content = $"ARG VERSION={versionString}\n{content}";
                }
            }
            else
            {
                // Update existing ARG VERSION
                content = Regex.Replace(content, 
                    @"^(\s*)ARG\s+VERSION\s*=.*", 
                    $"$1ARG VERSION={versionString}", 
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }
            
            // Add or update version comment
            if (!hasVersionComment)
            {
                // Add comment at the top
                content = $"# Version: {versionString}\n{content}";
            }
            else
            {
                // Update existing comment
                content = Regex.Replace(content,
                    @"#\s*Version:\s*.*",
                    $"# Version: {versionString}",
                    RegexOptions.IgnoreCase);
            }
            
            _fileOperations.WriteFileContent(filePath, content);
        }
        
        private void VersionNuGetFile(string filePath, string version, string prereleaseSuffix)
        {
            if (!_fileOperations.FileExists(filePath))
            {
                _log.Warning("File not found: {file}", filePath);
                return;
            }
            
            var content = _fileOperations.ReadFileContent(filePath);
            var doc = XDocument.Parse(content);
            var ns = XNamespace.Get("http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd");
            
            var package = doc.Element(ns + "package");
            if (package == null)
            {
                package = doc.Element("package");
                ns = XNamespace.None;
            }
            
            if (package != null)
            {
                var metadata = package.Element(ns + "metadata");
                if (metadata != null)
                {
                    var versionElement = metadata.Element(ns + "version");
                    var versionString = string.IsNullOrEmpty(prereleaseSuffix) ? version : $"{version}-{prereleaseSuffix}";
                    
                    if (versionElement != null)
                    {
                        versionElement.Value = versionString;
                    }
                    else
                    {
                        metadata.Add(new XElement(ns + "version", versionString));
                    }
                    
                    _fileOperations.WriteFileContent(filePath, doc.ToString());
                    _log.Debug("Updated version in .nuspec to {version}", versionString);
                }
            }
        }
        
        private void CreateVersionerPropsFile(string filePath, VersionedSetModel versionedSetModel, string prereleaseSuffix)
        {
            // This method is no longer used - we now use Directory.Build.props or modify .csproj files directly
            // Keeping for backward compatibility, but it should not be called in the new logic
            var versionString = string.IsNullOrEmpty(prereleaseSuffix) 
                ? versionedSetModel.AssemblyVersion 
                : $"{versionedSetModel.AssemblyVersion}-{prereleaseSuffix}";
            
            var propsContent = $@"<Project>
  <PropertyGroup>
    <Version>{versionedSetModel.AssemblyVersion}</Version>
    <AssemblyVersion>{versionedSetModel.AssemblyVersion}</AssemblyVersion>
    <FileVersion>{versionedSetModel.AssemblyFileVersion}</FileVersion>
    <AssemblyInformationalVersion>{versionedSetModel.AssemblyInfoVersion}</AssemblyInformationalVersion>
  </PropertyGroup>
</Project>";
            
            _fileOperations.WriteFileContent(filePath, propsContent);
            _log.Information("Created Versioner.props file with version: {version}", versionString);
        }
        
        private void RemoveVersionFromCsProj(string filePath)
        {
            if (!_fileOperations.FileExists(filePath))
            {
                _log.Warning("File not found: {file}", filePath);
                return;
            }
            
            var content = _fileOperations.ReadFileContent(filePath);
            var project = XDocument.Parse(content);
            
            // Remove version properties from all PropertyGroups
            var propertyGroups = project.Descendants()
                .Where(e => e.Name.LocalName == "PropertyGroup")
                .ToList();
            
            bool modified = false;
            foreach (var propertyGroup in propertyGroups)
            {
                // Remove version-related properties
                var versionElements = propertyGroup.Descendants()
                    .Where(e => e.Name.LocalName == "Version" || 
                               e.Name.LocalName == "AssemblyVersion" || 
                               e.Name.LocalName == "FileVersion" || 
                               e.Name.LocalName == "AssemblyInformationalVersion" ||
                               e.Name.LocalName == "VersionPrefix" ||
                               e.Name.LocalName == "VersionSuffix" ||
                               e.Name.LocalName == "PackageVersion")
                    .ToList();
                
                foreach (var element in versionElements)
                {
                    element.Remove();
                    modified = true;
                }
            }
            
            if (modified)
            {
                // Clean up empty elements
                foreach (var child in project.Descendants().Reverse())
                {
                    if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) 
                        child.Remove();
                }
                
                var projectContent = project.ToString(SaveOptions.None)
                    .Replace("-&gt;", "->")
                    .Replace("<ProjectGuid xmlns=\"\">", "<ProjectGuid>");
                _fileOperations.WriteFileContent(filePath, projectContent);
                _log.Debug("Removed version properties from: {file}", filePath);
            }
        }
    }
}

